#Requires -Version 7
# Stop and SubagentStop hook: formats the backend changed files, builds the solution, and reports build
# warnings and errors in those files back to the agent that stops. It also reports C# lines longer than
# max_line_length in .editorconfig (Migrations excluded). Exit code 2 blocks the stop and sends stderr to
# that agent, so a subagent fixes its own diagnostics before it reports to the main session.
# The hook uses git only. It resolves the checkout (the main one or a git worktree) from the hook input cwd with
# "git -C <cwd> rev-parse --show-toplevel" and exits when cwd is outside a checkout. It never falls back to
# CLAUDE_PROJECT_DIR or to the script folder, and it reads no transcript. The changed files are the files that git
# reports as changed in that checkout (git diff --name-only HEAD plus the untracked files).
# A changed file counts as new when its content hash differs from the hash saved at the previous stop. The hash
# state lives in the git directory of the checkout, so uncommitted changes from earlier stops do not trigger a
# rebuild. Limit: sessions that share one checkout share this state and these files. Give each session its own
# worktree to separate them.
# One verification or cleanup per checkout runs at a time (named mutex), and the cleanup never overwrites a file
# that another writer changed or deleted during the run (cleanup-changed.ps1).
# The hook does nothing (no build, no cleanup, no report) when the working tree equals the fingerprint that
# record-turn-start.ps1 stored at the start of the turn (UserPromptSubmit, by session_id), or the fingerprint of the
# last clean verification of the checkout. The fingerprint is the git status plus the git hash of each listed file
# (worktree-fingerprint.ps1).
# The hook blocks once per stop attempt. When stop_hook_active is set, it reports and lets the turn end,
# so a warning Claude cannot fix does not loop. Files with diagnostics stay out of the saved state,
# so the next stop checks them again.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'worktree-fingerprint.ps1')

$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$solutionPath = 'backend/HealthyFac.slnx'

# The checkout (main or worktree) that holds the directory, in the backslash form that the build diagnostics use,
# or nothing when the directory is not inside a checkout.
function Get-WorktreeRoot([string] $directory) {
  if (-not $directory -or -not (Test-Path -LiteralPath $directory -PathType Container)) { return }
  $root = git -C $directory rev-parse --show-toplevel 2>$null
  if ($LASTEXITCODE -ne 0 -or -not $root) { return }
  [IO.Path]::GetFullPath($root).TrimEnd([char]92, [char]47)
}

# The named mutex of a checkout, shared with cleanup-changed.ps1.
function New-CheckoutMutex([string] $repo) {
  $repoHash = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repo.ToLowerInvariant())))
  [Threading.Mutex]::new($false, "Local\HealthyFac-verify-$($repoHash.Substring(0, 16))")
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
  $patterns = '*.ts', '*.html', '*.css', '*.json', '*.webmanifest'
  $tracked = @(git diff --name-only HEAD -- $patterns 2>$null)
  $untracked = @(git ls-files -o --exclude-standard -- $patterns 2>$null)

  @($tracked + $untracked) |
    Where-Object { $_ -like 'frontend/*' -and $_ -notlike '*/package-lock.json' } |
    Sort-Object -Unique |
    Where-Object { Test-Path (Join-Path $repo $_) }
}

# Formats the frontend changed files with Prettier, then lints the frontend. The hash state works as in
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
        @($output | Where-Object { $_ -match '\S' -and $_ -notmatch '^> ' } | Select-Object -First 40)
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
  $lock = New-CheckoutMutex $repo
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
      $cleanupNotes = @(& (Join-Path $PSScriptRoot 'cleanup-changed.ps1') -Files $touchedCode -Repo $repo |
          Where-Object { $_ -like 'Cleanup *' })
      if ($cleanupNotes.Count -gt 0) { $result.Notice = $cleanupNotes -join [Environment]::NewLine }
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

$worktree = Get-WorktreeRoot $hookInput.cwd
if (-not $worktree -or -not (Test-Path (Join-Path $worktree $solutionPath))) { exit 0 }
$checkouts = @($worktree)

# The skip: no file changed since the start of the turn, or since the last clean verification.
$fingerprint = Get-WorktreeFingerprint $worktree
$turnPath = if ($hookInput.session_id) { Get-TurnFingerprintPath $worktree $hookInput.session_id }
$verifiedPath = Get-GitStatePath $worktree 'claude-verify-fingerprint.txt'
$isUnchangedInTurn = $turnPath -and (Test-Path -LiteralPath $turnPath) -and
  ((Get-Content -LiteralPath $turnPath -Raw).Trim() -eq $fingerprint)
$isUnchangedSinceVerified = (Test-Path -LiteralPath $verifiedPath) -and
  ((Get-Content -LiteralPath $verifiedPath -Raw).Trim() -eq $fingerprint)
if ($isUnchangedInTurn -or $isUnchangedSinceVerified) { exit 0 }

# One lock around the whole run: the frontend step formats live files and writes its own state, so it belongs to the
# same critical section as the backend step. The mutex is re-entrant, so Invoke-Verification and the cleanup take it
# again in the same thread.
$outerLock = New-CheckoutMutex $worktree
$hasOuterLock = try { $outerLock.WaitOne([TimeSpan]::FromSeconds(480)) } catch [Threading.AbandonedMutexException] { $true }
if (-not $hasOuterLock) {
  $outerLock.Dispose()
  Write-Output "Verification of $worktree skipped: another run held the lock for 480 seconds. The next stop checks the files again."
  exit 0
}
try {
  $results = @(foreach ($checkout in $checkouts) { Invoke-Verification $checkout; Invoke-FrontendVerification $checkout })
}
finally {
  $outerLock.ReleaseMutex()
  $outerLock.Dispose()
}
$results | Where-Object Notice | ForEach-Object { Write-Output $_.Notice }

$failed = @($results | Where-Object { $_.Problems.Count -gt 0 })
if ($failed.Count -eq 0) {
  New-Item -ItemType Directory -Force (Split-Path $verifiedPath) | Out-Null
  Set-Content -LiteralPath $verifiedPath -Value (Get-WorktreeFingerprint $worktree) -Encoding utf8NoBOM
  exit 0
}

$report = @(
  'The build, the line length check, or the frontend lint reports problems in the changed files of the checkout. ' +
    'Fix every item before you finish.'
  foreach ($result in $failed) {
    "Changed files in $($result.Repo):"
    $result.Problems
  }
) -join [Environment]::NewLine

if ($hookInput.stop_hook_active) {
  Write-Output "Verification still fails after one fix attempt:$([Environment]::NewLine)$report"
  exit 0
}

[Console]::Error.WriteLine($report)
exit 2
