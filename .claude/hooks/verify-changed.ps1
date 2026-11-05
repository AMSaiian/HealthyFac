#Requires -Version 7
# Stop and SubagentStop hook: formats the backend files touched this turn, builds the solution, and reports build
# warnings and errors in those files back to the agent that stops. It also reports C# lines longer than
# max_line_length in .editorconfig (Migrations excluded). Exit code 2 blocks the stop and sends stderr to
# that agent, so a subagent fixes its own diagnostics before it reports to the main session.
# The hook checks each checkout (the main one or a git worktree) that holds a file the stopping agent changed with
# Edit, Write, MultiEdit, or NotebookEdit, read from that agent's transcript. Only when it finds none does it fall
# back to the checkout of the hook input cwd, then to CLAUDE_PROJECT_DIR.
# A file counts as touched when its content hash differs from the hash saved at the previous stop. The hash
# state lives in the .git directory, so edits made outside Edit/Write (e.g. sed) count too, and uncommitted
# changes from earlier turns do not trigger a rebuild.
# The hook blocks once per stop attempt. When stop_hook_active is set, it reports and lets the turn end,
# so a warning Claude cannot fix does not loop. Files with diagnostics stay out of the saved state,
# so the next stop checks them again.

$ErrorActionPreference = 'Stop'

$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$solutionPath = 'backend/HealthyFac.slnx'
$editTools = 'Edit', 'Write', 'MultiEdit', 'NotebookEdit'

# Walks up from a path to the nearest directory that holds .git: a folder in the main checkout, a file in a
# worktree. The path can name a file that no longer exists. GetFullPath gives the backslash form that the build
# diagnostics use.
function Get-CheckoutRoot([string] $path) {
  if (-not $path) { return }
  $dir = $path
  while ($dir -and -not (Test-Path -LiteralPath $dir -PathType Container)) { $dir = Split-Path $dir }
  while ($dir) {
    if (Test-Path -LiteralPath (Join-Path $dir '.git')) { return [IO.Path]::GetFullPath($dir).TrimEnd('\', '/') }
    $dir = Split-Path $dir
  }
}

# A subagent writes its own transcript (agent_transcript_path); transcript_path names the main session. The string
# checks skip the large tool result lines before the JSON parse, so a long transcript takes well under a second.
function Get-EditedCheckouts {
  $transcript = if ($hookInput.agent_transcript_path) { $hookInput.agent_transcript_path } else {
    $hookInput.transcript_path
  }
  if (-not $transcript -or -not (Test-Path -LiteralPath $transcript)) { return }

  $dirs = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  foreach ($line in [IO.File]::ReadLines($transcript)) {
    if ($line.IndexOf('"type":"tool_use"', [StringComparison]::Ordinal) -lt 0) { continue }
    if ($line -notmatch '"name":"(Edit|Write|MultiEdit|NotebookEdit)"') { continue }
    $entry = try { $line | ConvertFrom-Json -Depth 32 } catch { continue }
    if ($entry.type -ne 'assistant') { continue }
    foreach ($content in @($entry.message.content)) {
      if ($content.type -ne 'tool_use' -or $content.name -notin $editTools) { continue }
      $file = if ($content.input.file_path) { $content.input.file_path } else { $content.input.notebook_path }
      if ($file) { [void]$dirs.Add((Split-Path $file)) }
    }
  }

  $dirs | ForEach-Object { Get-CheckoutRoot $_ } | Sort-Object -Unique
}

function Get-ChangedBackendFiles([string] $repo) {
  $patterns = '*.cs', '*.csproj', '*.props'
  $tracked = @(git diff --name-only HEAD -- $patterns 2>$null)
  $untracked = @(git ls-files -o --exclude-standard -- $patterns 2>$null)

  @($tracked + $untracked) |
    Where-Object { $_ -like 'backend/*' } |
    Sort-Object -Unique |
    Where-Object { Test-Path (Join-Path $repo $_) }
}

function Get-ContentHashes([string[]] $files) {
  $hashes = @{}
  if ($files.Count -eq 0) { return $hashes }
  $values = @(git hash-object -- $files)
  for ($i = 0; $i -lt $files.Count; $i++) { $hashes[$files[$i]] = $values[$i] }
  $hashes
}

function Get-ChangedFrontendFiles([string] $repo) {
  $patterns = '*.ts', '*.html', '*.css', '*.json'
  $tracked = @(git diff --name-only HEAD -- $patterns 2>$null)
  $untracked = @(git ls-files -o --exclude-standard -- $patterns 2>$null)

  @($tracked + $untracked) |
    Where-Object { $_ -like 'frontend/*' -and $_ -notlike '*/package-lock.json' } |
    Sort-Object -Unique |
    Where-Object { Test-Path (Join-Path $repo $_) }
}

# Formats the frontend files touched this turn with Prettier, then lints the frontend. The hash state works as in
# Invoke-Verification, in its own file. A lint failure lists the ESLint output lines. The hook runs no build or test.
function Invoke-FrontendVerification([string] $repo) {
  $result = [pscustomobject]@{ Repo = $repo; Problems = @(); Notice = $null }
  $frontend = Join-Path $repo 'frontend'
  if (-not (Test-Path (Join-Path $frontend 'node_modules'))) { return $result }

  Push-Location $repo
  try {
    $statePath = [IO.Path]::GetFullPath((git rev-parse --git-path claude-verify-frontend-state.json), $repo)
    $saved = @{}
    if (Test-Path $statePath) {
      (Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable).GetEnumerator() |
        ForEach-Object { $saved[$_.Key] = $_.Value }
    }

    $files = @(Get-ChangedFrontendFiles $repo)
    $current = Get-ContentHashes $files
    $touched = @($files | Where-Object { $saved[$_] -ne $current[$_] })
    if ($touched.Count -eq 0) { return $result }

    Push-Location $frontend
    try {
      $relative = $touched | ForEach-Object { $_.Substring('frontend/'.Length) }
      & npx prettier --write --ignore-unknown @relative 2>&1 | Out-Null
      $output = & npm run lint 2>&1 | ForEach-Object { "$_" }
      $lintFailed = $LASTEXITCODE -ne 0
    }
    finally { Pop-Location }

    $state = Get-ContentHashes @(Get-ChangedFrontendFiles $repo)
    if ($lintFailed) { $state = @{} }
    New-Item -ItemType Directory -Force (Split-Path $statePath) | Out-Null
    $state | ConvertTo-Json -Compress | Set-Content $statePath -Encoding utf8NoBOM

    if ($lintFailed) {
      $result.Problems = @('Frontend lint failed (npm run lint in frontend/):') +
        @($output | Where-Object { $_ -match 'S' -and $_ -notmatch '^> ' } | Select-Object -First 40)
    }
    return $result
  }
  finally { Pop-Location }
}

# Reports each line of the given C# files that is longer than max_line_length in .editorconfig, in the build
# diagnostic format. The JetBrains formatter wraps only at syntax points, so a long string literal or a one-line XML
# doc comment stays too long. The check skips the Migrations folders, because EF Core generates those files.
function Get-LongLineDiagnostics([string] $repo, [string[]] $files) {
  $editorConfig = Join-Path $repo '.editorconfig'
  if (-not (Test-Path $editorConfig)) { return }
  $setting = Select-String -LiteralPath $editorConfig -Pattern '^\s*max_line_length\s*=\s*(\d+)' |
    Select-Object -First 1
  if (-not $setting) { return }
  $limit = [int]$setting.Matches[0].Groups[1].Value

  foreach ($file in @($files | Where-Object { $_ -notlike '*/Migrations/*' })) {
    $path = Join-Path $repo $file
    if (-not (Test-Path -LiteralPath $path)) { continue }
    $number = 0
    foreach ($line in [IO.File]::ReadLines($path)) {
      $number++
      if ($line.Length -le $limit) { continue }
      "$($file.Replace('/', '\'))($number,$($limit + 1)): error LINE_LENGTH: the line has $($line.Length) " +
        "characters; .editorconfig max_line_length is $limit. Split the line."
    }
  }
}

# Formats and builds one checkout. Returns its diagnostics (empty when clean) and a notice for the transcript.
function Invoke-Verification([string] $repo) {
  $result = [pscustomobject]@{ Repo = $repo; Problems = @(); Notice = $null }
  $solution = Join-Path $repo $solutionPath

  # Parallel subagents can stop at the same time. A named mutex per checkout runs one verification at a time, so
  # two builds do not lock each other's output files and two runs do not overwrite the hash state. The wait stays
  # below the hook timeout. A run that the hook timeout kills leaves the mutex abandoned, and the next run takes it.
  $repoHash = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repo.ToLowerInvariant())))
  $lock = [Threading.Mutex]::new($false, "Local\HealthyFac-verify-$($repoHash.Substring(0, 16))")
  $lockWait = [TimeSpan]::FromSeconds(480)
  $hasLock = try { $lock.WaitOne($lockWait) } catch [Threading.AbandonedMutexException] { $true }
  if (-not $hasLock) {
    $lock.Dispose()
    $result.Notice = "Verification of $repo skipped: another run held the lock for " +
      "$($lockWait.TotalSeconds) seconds. The next stop checks the files again."
    return $result
  }

  Push-Location $repo
  try {
    # --git-path is relative in the main checkout and absolute in a worktree (.git/worktrees/<name>/).
    $statePath = [IO.Path]::GetFullPath((git rev-parse --git-path claude-verify-state.json), $repo)
    $saved = @{}
    if (Test-Path $statePath) {
      (Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable).GetEnumerator() |
        ForEach-Object { $saved[$_.Key] = $_.Value }
    }

    $files = @(Get-ChangedBackendFiles $repo)
    $current = Get-ContentHashes $files

    $touched = @($files | Where-Object { $saved[$_] -ne $current[$_] })
    $deleted = @($saved.Keys | Where-Object { -not (Test-Path (Join-Path $repo $_)) })

    if ($touched.Count -eq 0 -and $deleted.Count -eq 0) { return $result }

    $touchedCode = @($touched | Where-Object { $_ -like '*.cs' })
    if ($touchedCode.Count -gt 0) {
      & (Join-Path $PSScriptRoot 'cleanup-changed.ps1') -Files $touchedCode -Repo $repo | Out-Null
    }

    # The hook builds into its own artifacts folder (gitignored, outside the backend Docker context). A test run in
    # another session locks its DLLs in bin/, and a build into the same folder then fails with MSB3027 and blocks
    # the stop for a reason the agent cannot fix. Each worktree has its own folder, so worktrees never share files.
    $artifacts = Join-Path $repo 'artifacts/claude-verify'
    $output = & dotnet build $solution -nologo -clp:NoSummary --artifacts-path $artifacts 2>&1 |
      ForEach-Object { "$_" }
    $buildFailed = $LASTEXITCODE -ne 0

    $touchedPaths = $touched | ForEach-Object { (Join-Path $repo $_).Replace('/', '\').ToLowerInvariant() }
    $flagged = [Collections.Generic.HashSet[string]]::new()

    $diagnostics = @($output |
      Where-Object { $_ -match '^(?<file>.+?)\(\d+,\d+\): (warning|error) ' } |
      Where-Object { $buildFailed -or $touchedPaths -contains $Matches.file.ToLowerInvariant() } |
      ForEach-Object {
        $line = ($_ -replace ' \[[^\]]+\]$', '').Replace("$repo\", '')
        [void]$flagged.Add(($line -replace '\(\d+,\d+\): .*$', '').Replace('\', '/'))
        $line
      } |
      Sort-Object -Unique)

    $longLines = @(Get-LongLineDiagnostics $repo $touchedCode)
    foreach ($line in $longLines) { [void]$flagged.Add(($line -replace '\(\d+,\d+\): .*$', '').Replace('\', '/')) }

    $state = Get-ContentHashes @(Get-ChangedBackendFiles $repo)
    if ($buildFailed) { $state = @{} }
    foreach ($file in $flagged) { $state.Remove($file) }
    New-Item -ItemType Directory -Force (Split-Path $statePath) | Out-Null
    $state | ConvertTo-Json -Compress | Set-Content $statePath -Encoding utf8NoBOM

    if ($buildFailed -and $diagnostics.Count -eq 0) { $diagnostics = @($output | Select-Object -Last 20) }
    $result.Problems = @($diagnostics + $longLines)
    return $result
  }
  finally {
    Pop-Location
    $lock.ReleaseMutex()
    $lock.Dispose()
  }
}

$checkouts = @(Get-EditedCheckouts | Where-Object { Test-Path (Join-Path $_ $solutionPath) })
if ($checkouts.Count -eq 0) {
  $fallback = Get-CheckoutRoot $hookInput.cwd
  if (-not $fallback) { $fallback = Get-CheckoutRoot $env:CLAUDE_PROJECT_DIR }
  if (-not $fallback) { $fallback = Get-CheckoutRoot $PSScriptRoot }
  $checkouts = @($fallback | Where-Object { $_ -and (Test-Path (Join-Path $_ $solutionPath)) })
}
if ($checkouts.Count -eq 0) { exit 0 }

$results = @(foreach ($checkout in $checkouts) { Invoke-Verification $checkout; Invoke-FrontendVerification $checkout })
$results | Where-Object Notice | ForEach-Object { Write-Output $_.Notice }

$failed = @($results | Where-Object { $_.Problems.Count -gt 0 })
if ($failed.Count -eq 0) { exit 0 }

$report = @(
  'The build, the line length check, or the frontend lint reports problems in the files touched this turn. ' +
    'Fix every item before you finish.'
  foreach ($result in $failed) {
    if ($checkouts.Count -gt 1) { "In $($result.Repo):" }
    $result.Problems
  }
) -join [Environment]::NewLine

if ($hookInput.stop_hook_active) {
  Write-Output "Verification still fails after one fix attempt:$([Environment]::NewLine)$report"
  exit 0
}

[Console]::Error.WriteLine($report)
exit 2
