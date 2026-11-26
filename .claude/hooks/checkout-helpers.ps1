# checkout-helpers.ps1 - dot-sourced by verify-changed.ps1, record-turn-start.ps1, tree-id.ps1, check-handoff.ps1, and
# run-area.ps1: the one definition of the tree ID, the state paths, the checkout mutex, and the long-line check. Git only.
# cleanup-changed.ps1 keeps its own copy of the mutex name (Artur, 2026-11-21: the cleanup stays unchanged); both must
# build the same name, so the mutex is re-entrant for the hook and the cleanup in one thread.

# One git tree ID for the working tree of a checkout: every tracked and every untracked file that .gitignore does not
# exclude. Two equal IDs mean the same content. -Area limits the tree to one test area:
#   frontend  the folders frontend/ and keycloak-theme/;
#   backend   every other path, except .claude/ and CLAUDE.md (the agent tools: their script tests cover them);
#   all       the whole tree.
# -Ref reads the tree of a commit instead of the working tree. The function works on a copy of the index
# (GIT_INDEX_FILE), so the real index, every ref, and every file stay as they are. The written blobs and trees stay as
# loose objects; `git gc` prunes them after 2 weeks. It throws when git fails.
function Get-TreeId([string] $Repo, [string] $Area = 'all', [string] $Ref) {
  $frontendPaths = @('frontend', 'keycloak-theme')
  $toolPaths = @('.claude', 'CLAUDE.md')
  $indexPath = (git -C $Repo rev-parse --path-format=absolute --git-path index 2>$null)
  if ($LASTEXITCODE -ne 0 -or -not $indexPath) { throw "No git checkout: $Repo" }
  $indexPath = $indexPath.Trim()

  $temporaryIndex = Join-Path ([IO.Path]::GetTempPath()) "hf-tree-id-$([Guid]::NewGuid().ToString('N')).idx"
  $previousIndex = $env:GIT_INDEX_FILE
  try {
    $env:GIT_INDEX_FILE = $temporaryIndex
    if ($Ref) {
      git -C $Repo read-tree $Ref 2>$null | Out-Null
      if ($LASTEXITCODE -ne 0) { throw "git read-tree $Ref failed." }
    } else {
      if (Test-Path -LiteralPath $indexPath) { Copy-Item -LiteralPath $indexPath -Destination $temporaryIndex }
      git -C $Repo add -A 2>$null | Out-Null
      if ($LASTEXITCODE -ne 0) { throw 'git add failed.' }
    }
    $excluded = switch ($Area) {
      'backend' { $frontendPaths + $toolPaths }
      'frontend' { @('.') + @($frontendPaths | ForEach-Object { ":(exclude)$_" }) }
      'all' { @() }
      default { throw "Unknown area: $Area" }
    }
    if ($excluded.Count -gt 0) {
      # -f: with -Ref, the copy holds the base content, which differs from HEAD and from the files after a commit on the
      # branch, and git rm refuses such entries without it. --cached on a copy of the index touches no file.
      git -C $Repo rm -r -q -f --cached --ignore-unmatch -- @excluded 2>$null | Out-Null
      if ($LASTEXITCODE -ne 0) { throw 'git rm --cached failed.' }
    }
    $tree = git -C $Repo write-tree 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $tree) { throw 'git write-tree failed.' }
    $tree.Trim()
  }
  finally {
    if ($null -eq $previousIndex) { Remove-Item Env:GIT_INDEX_FILE -ErrorAction SilentlyContinue } else { $env:GIT_INDEX_FILE = $previousIndex }
    Remove-Item -LiteralPath $temporaryIndex, "$temporaryIndex.lock" -Force -ErrorAction SilentlyContinue
  }
}

# The output lines and the exit code of a git command that prints paths, with core.quotepath=false (raw UTF-8 paths).
# PowerShell decodes native output with [Console]::OutputEncoding, which is the OEM code page in a redirected process
# (e.g. cp866), so a path with non-ASCII characters would not match its file. The call reads UTF-8 and restores both
# encodings after it.
function Get-GitLines([string] $Repo, [string[]] $Arguments) {
  $previousConsole = [Console]::OutputEncoding
  $previousPipe = $OutputEncoding
  $utf8 = [Text.UTF8Encoding]::new($false)
  try {
    [Console]::OutputEncoding = $utf8
    $OutputEncoding = $utf8
    $lines = @(git -C $Repo -c core.quotepath=false @Arguments 2>$null)
    [pscustomobject]@{ Lines = $lines; ExitCode = $LASTEXITCODE }
  }
  finally {
    [Console]::OutputEncoding = $previousConsole
    $OutputEncoding = $previousPipe
  }
}

# The absolute path of a state file in the git directory of the checkout (relative in the main checkout, absolute in a
# worktree).
function Get-GitStatePath([string] $repo, [string] $name) {
  [IO.Path]::GetFullPath((git -C $repo rev-parse --git-path $name), $repo)
}

# The file that holds the tree ID of a session at the start of its turn.
function Get-TurnTreePath([string] $repo, [string] $sessionId) {
  $safe = $sessionId -replace '[^A-Za-z0-9_-]', '_'
  Get-GitStatePath $repo "claude-turn-$safe.txt"
}

# Deletes the turn files that no session has written for longer than $maxAge. Each prompt of a live session rewrites
# its file, so only the files of ended sessions age out.
function Remove-StaleTurnStates([string] $repo, [TimeSpan] $maxAge) {
  $directory = Split-Path (Get-GitStatePath $repo 'claude-turn-x.txt')
  if (-not (Test-Path -LiteralPath $directory -PathType Container)) { return }
  $limit = [DateTime]::UtcNow - $maxAge
  Get-ChildItem -LiteralPath $directory -Filter 'claude-turn-*.txt' -File |
    Where-Object { $_.LastWriteTimeUtc -lt $limit } |
    Remove-Item -Force -ErrorAction SilentlyContinue
}

# The named mutex of a checkout. cleanup-changed.ps1 builds the same name.
function New-CheckoutMutex([string] $repo) {
  $repoHash = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repo.ToLowerInvariant())))
  [Threading.Mutex]::new($false, "Local\HealthyFac-verify-$($repoHash.Substring(0, 16))")
}

# One object (File, Line, Length, Limit) for each line of the given C# files (repository-relative) that is longer than
# max_line_length of .editorconfig. The JetBrains cleanup wraps only at syntax points, so a long string literal or a
# one-line XML doc comment stays too long. Files under Migrations/ are skipped, because EF Core generates them.
function Get-LongLines([string] $Repo, [string[]] $Files) {
  $editorConfig = Join-Path $Repo '.editorconfig'
  if (-not (Test-Path -LiteralPath $editorConfig)) { return }
  $setting = Select-String -LiteralPath $editorConfig -Pattern '^\s*max_line_length\s*=\s*(\d+)' | Select-Object -First 1
  if (-not $setting) { return }
  $limit = [int]$setting.Matches[0].Groups[1].Value

  foreach ($file in @($Files | Where-Object { $_ -like '*.cs' -and $_ -notmatch '(^|/)Migrations/' })) {
    $path = Join-Path $Repo $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $number = 0
    foreach ($line in [IO.File]::ReadLines($path)) {
      $number++
      if ($line.Length -le $limit) { continue }
      [pscustomobject]@{ File = $file; Line = $number; Length = $line.Length; Limit = $limit }
    }
  }
}

# The diagnostics of the build as typed lines, one per warning or error:
#   <repo path>(<line>,<column>): <level> <rule>: <message>
# The lines come from every *.sarif file under -Root (the compiler writes one per project into obj/, see
# backend/Directory.Build.props), sorted, without duplicates (a shared file shows in the SARIF of each project that
# compiles it). A suppressed result and a result under /obj/ (generated code) are out, and so is a result of another
# level than warning or error (the SARIF also holds the info and hidden diagnostics). The path is relative to -Repo with
# forward slashes. -Files keeps only the results of those repo paths. Each line of -BuildOutput with an NU or MSB
# warning or error is added without its trailing [<project>] (NuGet and MSBuild write no SARIF); with
# -BuildOutputLevel error, only the errors of those lines. -SkipRules leaves
# those rule IDs out and writes their count to $SkippedCount.
function Get-SarifDiagnostics(
  [string] $Repo, [string] $Root, [string[]] $BuildOutput = @(), [string[]] $SkipRules = @(), [string[]] $Files = @(),
  [ref] $SkippedCount, [ValidateSet('error', 'all')] [string] $BuildOutputLevel = 'all') {
  $repoFull = [IO.Path]::GetFullPath($Repo).TrimEnd('\', '/').Replace('\', '/')
  $wanted = @($Files | ForEach-Object { $_.Replace('\', '/').ToLowerInvariant() })
  $lines = [Collections.Generic.HashSet[string]]::new()
  $skipped = 0

  $sarifFiles = if (Test-Path -LiteralPath $Root -PathType Container) {
    @(Get-ChildItem -LiteralPath $Root -Filter '*.sarif' -File -Recurse)
  } else { @() }
  foreach ($sarifFile in $sarifFiles) {
    $log = Get-Content -LiteralPath $sarifFile.FullName -Raw | ConvertFrom-Json
    foreach ($result in @($log.runs[0].results)) {
      if (-not $result -or $result.suppressions -or $result.level -notin 'warning', 'error') { continue }
      $location = @($result.locations)[0].physicalLocation
      if (-not $location) { continue }
      $uri = $location.artifactLocation.uri
      if ($uri -match '/obj/') { continue }
      $path = [Uri]::new($uri).LocalPath.Replace('\', '/')
      if ($path.StartsWith("$repoFull/", [StringComparison]::OrdinalIgnoreCase)) { $path = $path.Substring($repoFull.Length + 1) }
      if ($wanted.Count -gt 0 -and $wanted -notcontains $path.ToLowerInvariant()) { continue }
      if ($SkipRules -contains $result.ruleId) { $skipped++; continue }
      $message = $result.message.text -replace '\s*[\r\n]+\s*', ' '
      [void]$lines.Add("$path($($location.region.startLine),$($location.region.startColumn)): $($result.level) $($result.ruleId): $message")
    }
  }
  $repoPrefix = '^' + [regex]::Escape($repoFull).Replace('/', '[\\/]') + '[\\/]'
  foreach ($line in $BuildOutput) {
    $levels = if ($BuildOutputLevel -eq 'error') { 'error' } else { 'warning|error' }
    if ($line -notmatch ": ($levels) (NU|MSB)\d+") { continue }
    $text = $line -replace ' \[[^\]]+\]$', ''
    $text = $text -replace $repoPrefix
    if ($text -match '^(?<path>.*?)(?<rest> ?: (warning|error) .*)$') { $text = $Matches.path.Replace('\', '/') + $Matches.rest }
    [void]$lines.Add($text)
  }
  if ($SkippedCount) { $SkippedCount.Value = $skipped }
  @($lines | Sort-Object { $_ })
}

# The rule IDs that the JetBrains cleanup fixes: the Stop hook, run-area.ps1, and run-tests.ps1 do not report them as
# build diagnostics.
function Get-CleanupFixedRules([string] $Repo) {
  $path = Join-Path $Repo '.claude/hooks/cleanup-fixed-rules.txt'
  if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return @() }
  @(Get-Content -LiteralPath $path | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}
