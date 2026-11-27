# checkout-helpers.ps1 - dot-sourced by verify-changed.ps1, record-turn-start.ps1, tree-id.ps1, check-handoff.ps1,
# check-plan.ps1, commit-cycle.ps1, run-area.ps1, run-tests.ps1, format-changed.ps1, start-feature.ps1,
# reviewer-write-scope.ps1, and reviewer-shell-scope.ps1: the one
# definition of the tree ID and the area paths, the main branch ref, the default base, the feature base, the state paths,
# the checkout mutex, the run results of run-area.ps1 and run-tests.ps1 (Write-RunResult, Get-RunResults), the Stop hook
# state after a run (Get-StepChanges, Save-StepState, Write-HookState), and the tool suites (Get-ToolSuites,
# Invoke-ToolSuites), and the run parts of both run scripts (the test module constants, Get-AreaChangedPaths,
# Invoke-RunCommand, Invoke-ToolingRun), and the scratch folder of the reviewer (ReviewerScratchFolder). Git only.
# cleanup-changed.ps1 keeps its own copy of the mutex name (Artur, 2026-11-21: the cleanup stays unchanged); both must
# build the same name, so the mutex is re-entrant for the hook and the cleanup in one thread.

# The path lists of the test areas, defined once for Get-TreeId and run-area.ps1: the frontend area, the agent tools
# that the backend area leaves out, and the tooling area.
$script:AreaPaths = @{
  frontend = @('frontend', 'keycloak-theme')
  agentTools = @('.claude', 'CLAUDE.md')
  tooling = @('.claude', 'CLAUDE.md', 'scripts')
}

# One git tree ID for the working tree of a checkout: every tracked and every untracked file that .gitignore does not
# exclude. Two equal IDs mean the same content. -Area limits the tree to one test area:
#   frontend  the folders frontend/ and keycloak-theme/;
#   backend   every other path, except .claude/ and CLAUDE.md (the agent tools: the tooling area covers them);
#   tooling   only .claude/, CLAUDE.md, and scripts/ (the agent tools and the verify stack);
#   all       the whole tree.
# -Ref reads the tree of a commit instead of the working tree. The function works on a copy of the index
# (GIT_INDEX_FILE), so the real index, every ref, and every file stay as they are. The written blobs and trees stay as
# loose objects; `git gc` prunes them after 2 weeks. It throws when git fails.
function Get-TreeId([string] $Repo, [string] $Area = 'all', [string] $Ref) {
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
      'backend' { $script:AreaPaths.frontend + $script:AreaPaths.agentTools }
      'frontend' { @('.') + @($script:AreaPaths.frontend | ForEach-Object { ":(exclude)$_" }) }
      'tooling' { @('.') + @($script:AreaPaths.tooling | ForEach-Object { ":(exclude)$_" }) }
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

# The ref of the main branch: origin/<MainBranch> when that remote-tracking ref exists, else the local <MainBranch>
# (the local branch can be behind origin). It never fetches. It writes the note to the host, not to the output, when it
# falls back ("Note: origin/<MainBranch> not found; <Use> the local <MainBranch>.").
function Get-MainBranchRef(
  [string] $Repo, [string] $MainBranch = 'develop', [string] $Use = 'the base is the merge base with') {
  git -C $Repo rev-parse --verify --quiet "refs/remotes/origin/$MainBranch" 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { return "origin/$MainBranch" }
  Write-Host "Note: origin/$MainBranch not found; $Use the local $MainBranch."
  return $MainBranch
}

# The base of a feature: the "Base:" SHA of the plan header (Get-PlanBase of plan-sections.ps1, which the caller
# loads), else Get-DefaultBase.
function Get-FeatureBase([string] $Repo, [string[]] $PlanLines, [string] $MainBranch = 'develop') {
  $planBase = Get-PlanBase $PlanLines
  if ($planBase) { return $planBase }
  return Get-DefaultBase $Repo $MainBranch
}

# The default base of a feature: the merge base of HEAD and origin/<MainBranch> when that remote-tracking ref exists,
# else the merge base with the local <MainBranch> (the local branch can be behind origin, so it can add the commits of
# other features to the diff). It never fetches. It writes the note to the host, not to the output, when it falls back.
# Returns nothing when git finds no merge base.
function Get-DefaultBase([string] $Repo, [string] $MainBranch = 'develop') {
  $ref = Get-MainBranchRef $Repo $MainBranch
  $base = git -C $Repo merge-base HEAD $ref 2>$null
  if ($LASTEXITCODE -ne 0 -or -not $base) { return }
  return $base.Trim()
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
  [IO.Path]::GetFullPath((git -C $repo rev-parse --git-path $name), (Resolve-Path -LiteralPath $repo).ProviderPath)
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

# The diagnostics of the build as typed lines, one per warning or error:
#   <repo path>(<line>,<column>): <level> <rule>: <message>
# The lines come from every *.sarif file under -Root (the compiler writes one per project into obj/, see
# backend/Directory.Build.props), sorted, without duplicates (a shared file shows in the SARIF of each project that
# compiles it). A suppressed result and a result under /obj/ (generated code) are out, and so is a result of another
# level than warning or error (the SARIF also holds the info and hidden diagnostics). The path is relative to -Repo with
# forward slashes. -Files keeps only the results of those repo paths. Each line of -BuildOutput with an NU or MSB
# warning or error is added without its trailing [<project>] (NuGet and MSBuild write no SARIF); with
# -BuildOutputLevel error, only the errors of those lines. -Since reads only the *.sarif files that were written at
# or after that time: after a failed build, MSBuild does not rewrite the file of a project that it skipped, so an old
# file holds results of an earlier build. Without -Since, every file counts.
function Get-SarifDiagnostics(
  [string] $Repo, [string] $Root, [string[]] $BuildOutput = @(), [string[]] $Files = @(),
  [ValidateSet('error', 'all')] [string] $BuildOutputLevel = 'all', [object] $Since = $null) {
  $sinceUtc = if ($null -ne $Since) { ([datetime]$Since).ToUniversalTime() }
  $repoFull = [IO.Path]::GetFullPath($Repo).TrimEnd('\', '/').Replace('\', '/')
  $wanted = @($Files | ForEach-Object { $_.Replace('\', '/').ToLowerInvariant() })
  $lines = [Collections.Generic.HashSet[string]]::new()

  $sarifFiles = if (Test-Path -LiteralPath $Root -PathType Container) {
    @(Get-ChildItem -LiteralPath $Root -Filter '*.sarif' -File -Recurse |
        Where-Object { $null -eq $sinceUtc -or $_.LastWriteTimeUtc -ge $sinceUtc })
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
  @($lines | Sort-Object { $_ })
}

# The file patterns of the hook steps (the backend step and the web projects).
$script:BackendPatterns = @('*.cs', '*.csproj', '*.props', '*.targets')
$script:WebPatterns = @('*.ts', '*.html', '*.css', '*.json', '*.webmanifest')

# The folder of the checkout (repo-relative, forward slashes) where the reviewer may write scratch files: both reviewer
# hooks (reviewer-write-scope.ps1, reviewer-shell-scope.ps1) read it. .gitignore ignores artifacts/.
$script:ReviewerScratchFolder = 'artifacts/claude-review'

# The paths of one step (folder $step/, or backend/) that changed since the last clean run of the step, deleted paths
# included. See the header for the state tree and the fallback.
function Get-StepChanges([string] $repo, [string] $step, [string] $currentTree, [string[]] $patterns) {
  $statePath = Get-GitStatePath $repo "claude-verify-$step-tree.txt"
  $stateTree = if (Test-Path -LiteralPath $statePath) { (Get-Content -LiteralPath $statePath -Raw).Trim() }
  $paths = $null
  if ($stateTree) {
    $diff = Get-GitLines $repo (@('diff', '--name-only', '--no-renames', $stateTree, $currentTree, '--') + $patterns)
    if ($diff.ExitCode -eq 0) { $paths = $diff.Lines }
  }
  if ($null -eq $paths) {
    $paths = @((Get-GitLines $repo (@('diff', '--name-only', '--no-renames', 'HEAD', '--') + $patterns)).Lines) +
      @((Get-GitLines $repo (@('ls-files', '-o', '--exclude-standard', '--') + $patterns)).Lines)
  }
  @($paths | Where-Object { $_ -like "$step/*" -and $_ -notlike '*/package-lock.json' } | Sort-Object -Unique)
}

function Save-StepState([string] $repo, [string] $step, [string] $tree) {
  $path = Get-GitStatePath $repo "claude-verify-$step-tree.txt"
  New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
  Set-Content -LiteralPath $path -Value $tree -Encoding utf8NoBOM
}

# Writes the Stop hook state after a passing run of a run script, so the hook finds the verified tree and formats and
# builds nothing (verify-changed.ps1). The backend step needs a clean hook check first: the SARIF diagnostics of the
# changed backend files that the build wrote, and their named index arguments (NAMED_INDEX). Else the lines print as "Hook check: <line>" and no state is written. The
# frontend step has no check here (run-area.ps1 ran its lint); keycloak-theme gets its state when -RanKeycloakTheme is
# set or none of its files changed since its state. Run it before the last line of the run.
function Write-HookState([string] $Repo, [string] $Area, [string[]] $BuildOutput = @(), [switch] $RanKeycloakTheme) {
  $tree = Get-TreeId -Repo $Repo
  if ($Area -eq 'backend') {
    $changed = @(Get-StepChanges $Repo 'backend' $tree $script:BackendPatterns |
        Where-Object { Test-Path -LiteralPath (Join-Path $Repo $_) -PathType Leaf })
    if ($changed.Count -gt 0) {
      $lines = @(Get-SarifDiagnostics -Repo $Repo -Root (Join-Path $Repo 'backend') -BuildOutput $BuildOutput `
          -BuildOutputLevel error -Files $changed)
      $lines += @(Get-NamedIndexArguments -Repo $Repo -Files $changed)
      if ($lines.Count -gt 0) {
        foreach ($line in $lines) { "Hook check: $line" }
        return
      }
    }
    Save-StepState $Repo 'backend' $tree
  }
  if ($Area -eq 'frontend') {
    Save-StepState $Repo 'frontend' $tree
    $isThemeUnchanged = @(Get-StepChanges $Repo 'keycloak-theme' $tree $script:WebPatterns).Count -eq 0
    if ($RanKeycloakTheme -or $isThemeUnchanged) { Save-StepState $Repo 'keycloak-theme' $tree }
  }
}

# The test script of each group of tool files (paths relative to the repo; a suite runs once when a changed path
# matches its pattern).
$script:ToolSuites = [ordered]@{
  '.claude/tests/rules.tests.ps1'            = '^(\.claude/.+\.(md|ps1)$|CLAUDE\.md$)'
  '.claude/hooks/tests/hook-scope.tests.ps1' = '^\.claude/hooks/'
  '.claude/tests/flow-scripts.tests.ps1'     = '^\.claude/[^/]+\.ps1$|^\.claude/tests/flow-scripts|^\.claude/hooks/(checkout-helpers|command-line-batches|cleanup-changed)\.ps1$'
  'scripts/tests/verify-slots.tests.ps1'     = '^scripts/(verify-|tests/verify-)'
  'scripts/tests/agent-chrome.tests.ps1'     = '^scripts/(tests/)?agent-chrome'
}

# The script test suites (paths relative to the repo) that the changed paths select and that exist in the repo.
function Get-ToolSuites([string] $Repo, [string[]] $Paths) {
  @($script:ToolSuites.Keys | Where-Object {
      $suite = $_
      $isMatch = @($Paths | Where-Object { $_ -match $script:ToolSuites[$suite] }).Count -gt 0
      $isMatch -and (Test-Path -LiteralPath (Join-Path $Repo $suite))
    })
}

# Runs the given script test suites at the same time, each with its own log in the claude-runs folder. It returns an
# object with Lines (one "<suite>: passed|failed (exit <n>); log <path>" line for each suite) and Failure (the first
# failing suite with its first FAIL line as "<suite> - <line>", else $null).
function Invoke-ToolSuites([string] $Repo, [string[]] $Suites, [string] $LogPrefix) {
  $logFolder = Get-GitStatePath $Repo 'claude-runs'
  New-Item -ItemType Directory -Force $logFolder | Out-Null
  $stamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
  $runs = foreach ($suite in $Suites) {
    $log = Join-Path $logFolder "$LogPrefix-$stamp-$(Split-Path $suite -Leaf).log"
    $process = Start-Process pwsh -ArgumentList @('-NoProfile', '-File', (Join-Path $Repo $suite)) -WorkingDirectory $Repo `
      -RedirectStandardOutput $log -RedirectStandardError "$log.err" -NoNewWindow -PassThru
    [pscustomobject]@{ Suite = $suite; Log = $log; Process = $process }
  }
  foreach ($run in $runs) { $run.Process.WaitForExit() }
  $lines = [Collections.Generic.List[string]]::new()
  $failure = $null
  foreach ($run in $runs) {
    $suiteLines = @(Get-Content -LiteralPath $run.Log -ErrorAction SilentlyContinue)
    $exitCode = $run.Process.ExitCode
    $result = if ($exitCode -eq 0) { 'passed' } else { 'failed' }
    $lines.Add("$(Split-Path $run.Suite -Leaf): $result (exit $exitCode); log $($run.Log)")
    if ($exitCode -ne 0 -and -not $failure) {
      $firstFail = @($suiteLines | Where-Object { $_ -match '^\s*FAIL ' } | Select-Object -First 1)
      $reason = if ($firstFail) { $firstFail[0].Trim() } else { @($suiteLines)[-1] }
      $failure = "$(Split-Path $run.Suite -Leaf) - $reason"
    }
  }
  [pscustomobject]@{ Lines = $lines; Failure = $failure }
}

# Stores the result of a run script in <git dir>/claude-runs/result-<area>-<full|affected>.json (each worktree has its
# own git directory). Mode is "full" (run-area.ps1) or "affected" (run-tests.ps1); Status is "pass" or "fail".
function Write-RunResult([string] $Repo, [string] $Mode, [string] $Area, [string] $Tree, [string] $Status, [string] $Line) {
  $folder = Get-GitStatePath $Repo 'claude-runs'
  New-Item -ItemType Directory -Force $folder | Out-Null
  [ordered]@{
    kind = if ($Mode -eq 'full') { 'Full run' } else { 'Affected run' }
    area = $Area
    tree = $Tree
    status = $Status
    line = $Line
    finished = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
  } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder "result-$Area-$Mode.json") -Encoding utf8
}

# The stored run results of the checkout as objects (kind, area, tree, status, line, finished).
function Get-RunResults([string] $Repo) {
  $folder = Get-GitStatePath $Repo 'claude-runs'
  if (-not (Test-Path -LiteralPath $folder)) { return @() }
  @(Get-ChildItem -LiteralPath $folder -Filter 'result-*.json' | Sort-Object Name |
      ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
}

# True when the repo path (forward slashes) belongs to the test area: backend (every path outside the frontend and the
# agent tools), frontend, or tooling.
function Test-PathInArea([string] $Path, [string] $Area) {
  $isFrontend = @($script:AreaPaths.frontend | Where-Object { $Path -eq $_ -or $Path -like "$_/*" }).Count -gt 0
  if ($Area -eq 'frontend') { return $isFrontend }
  if ($Area -eq 'tooling') { return @($script:AreaPaths.tooling | Where-Object { $Path -eq $_ -or $Path -like "$_/*" }).Count -gt 0 }
  $isTool = @($script:AreaPaths.agentTools | Where-Object { $Path -eq $_ -or $Path -like "$_/*" }).Count -gt 0
  return -not $isFrontend -and -not $isTool
}

# The passed count of a test command: the "succeeded:" line of the Microsoft Testing Platform summary, or the
# "Tests <n> passed" line of vitest. Nothing for a command without tests.
function Get-PassedCount([string[]] $Lines) {
  $succeeded = @($Lines | Select-String -Pattern '^\s*succeeded:\s*(\d+)' | Select-Object -Last 1)
  if ($succeeded) { return [int]$succeeded[0].Matches[0].Groups[1].Value }
  $vitest = @($Lines | Select-String -Pattern '^\s*Tests\s+(?:\d+ failed \| )?(\d+) passed' | Select-Object -Last 1)
  if ($vitest) { return [int]$vitest[0].Matches[0].Groups[1].Value }
}

function Get-FailedTests([string[]] $Lines) {
  @($Lines | Where-Object { $_ -match '^\s*failed\s+\S' -and $_ -notmatch '^\s*failed:' -or $_ -match '^\s*(FAIL|×)\s' } |
    ForEach-Object { $_.Trim() })
}

# The test modules of the backend commands and the number of integration projects that run at the same time (the
# commands of CLAUDE.md, Build and Test), defined once for run-area.ps1 and run-tests.ps1.
$script:UnitModules = 'backend/tests/**/bin/Debug/net10.0/HealthyFac.*.UnitTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ArchitectureTests.dll'
$script:IntegrationModules = 'backend/tests/**/bin/Debug/net10.0/HealthyFac.*.IntegrationTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ApiTests.dll'
$script:IntegrationParallelModules = 4

# The changed paths of the test area since the base (git diff plus untracked files), with forward slashes.
function Get-AreaChangedPaths([string] $Repo, [string] $Base, [string] $Area) {
  @(@((Get-GitLines $Repo @('diff', '--name-only', '--no-renames', $Base)).Lines) +
    @((Get-GitLines $Repo @('ls-files', '--others', '--exclude-standard')).Lines) |
      Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique |
      Where-Object { Test-PathInArea $_ $Area })
}

# Runs one command of a run script in its folder and writes its output to $Log. It returns an object with Lines (what the
# run script prints, in order: "<label>: passed|failed (exit <n>) -> <count> passed|no test count; log <path>", and for
# a failing command the failed tests; for a failing build also its SARIF diagnostics, errors first, before the failed
# tests), ExitCode, Count (the passed count or $null), and Output (the log lines without ANSI codes).
function Invoke-RunCommand([string] $Repo, [string] $Label, [string] $Folder, [string] $Tool, [string[]] $Arguments,
  [string] $Log) {
  $startedAt = [DateTime]::UtcNow
  Push-Location (Join-Path $Repo $Folder)
  try {
    & $Tool @Arguments *> $Log
    $exitCode = $LASTEXITCODE
  }
  finally { Pop-Location }
  $output = @(Get-Content -LiteralPath $Log -ErrorAction SilentlyContinue | ForEach-Object { $_ -replace '\x1b\[[0-9;]*m', '' })
  $count = Get-PassedCount $output
  $countText = if ($null -ne $count) { "$count passed" } else { 'no test count' }
  $result = if ($exitCode -eq 0) { 'passed' } else { 'failed' }
  $lines = [Collections.Generic.List[string]]::new()
  $lines.Add("${Label}: $result (exit $exitCode) -> $countText; log $Log")
  if ($Label -eq 'build' -and $exitCode -ne 0) {
    $diagnostics = @(Get-SarifDiagnostics -Repo $Repo -Root (Join-Path $Repo 'backend') -BuildOutput $output -Since $startedAt)
    foreach ($line in @($diagnostics | Where-Object { $_ -match ': error ' })) { $lines.Add($line) }
    foreach ($line in @($diagnostics | Where-Object { $_ -notmatch ': error ' })) { $lines.Add($line) }
  }
  if ($exitCode -ne 0) { foreach ($line in @(Get-FailedTests $output)) { $lines.Add($line) } }
  [pscustomobject]@{ Lines = $lines; ExitCode = $exitCode; Count = $count; Output = $output }
}

# The tooling run: the suites that the changed paths select (Get-ToolSuites) run at the same time, once each; the tree
# of the area is read before and after. Mode is "full" (run-area.ps1) or "affected" (run-tests.ps1). It returns an
# object with Lines (the suite lines to print), Failure (the reason of a failed run, else $null), Tree, and LastLine
# (the line of a passing run).
function Invoke-ToolingRun([string] $Repo, [string] $Area, [string[]] $Paths, [ValidateSet('full', 'affected')] [string] $Mode) {
  $treeBefore = Get-TreeId -Repo $Repo -Area $Area
  $suites = @(Get-ToolSuites $Repo $Paths)
  $logPrefix = if ($Mode -eq 'full') { $Area } else { "$Area-affected" }
  $run = Invoke-ToolSuites $Repo $suites $logPrefix
  $failure = $run.Failure
  if (-not $failure -and (Get-TreeId -Repo $Repo -Area $Area) -ne $treeBefore) { $failure = 'the tree changed during the run' }
  $names = if ($suites.Count -gt 0) { @($suites | ForEach-Object { Split-Path $_ -Leaf }) -join ', ' } else { 'none' }
  $kind = if ($Mode -eq 'full') { 'Full run' } else { 'Affected run' }
  [pscustomobject]@{
    Lines = $run.Lines; Failure = $failure; Tree = $treeBefore
    LastLine = "$kind ${Area}: tree $treeBefore; suites $names -> passed"
  }
}

# The regex of a glob of a Files line or of test-features.json: "**/" any folders (or none), "**" any path, "*" a name
# part. The regex matches a whole repo path (forward slashes).
function ConvertTo-GlobRegex([string] $Glob) {
  '^' + ([regex]::Escape($Glob) -replace '\\\*\\\*/', '(.*/)?' -replace '\\\*\\\*', '.*' -replace '\\\*', '[^/]*') + '$'
}

# The text of a C# file with every comment, string literal, and character literal replaced by spaces (line breaks stay),
# so a pattern over the result keeps the line and the column of the original. It reads //, /* */, "...", @"...",
# $"...", $@"...", raw """...""" strings ($$"""...""" too), and 'x'. The text parts of an interpolated string are
# masked, but each interpolation hole is code: it stays, and the strings and comments inside it are masked as before.
function Get-MaskedCSharp([string] $Text) {
  $state = @{ Text = $Text; Position = 0; Output = [Text.StringBuilder]::new($Text.Length) }
  Read-CSharpCode $state $false
  $state.Output.ToString()
}

function Add-MaskedRange([hashtable] $State, [int] $Start, [int] $End) {
  foreach ($char in $State.Text.Substring($Start, $End - $Start).ToCharArray()) {
    [void]$State.Output.Append($(if ($char -eq "`r" -or $char -eq "`n") { $char } else { ' ' }))
  }
}

# True when the "@" and "$" characters from $Position are followed by a double quote (a string prefix).
function Test-StringPrefix([string] $Text, [int] $Position) {
  while ($Position -lt $Text.Length -and $Text[$Position] -in '@', '$') { $Position++ }
  $Position -lt $Text.Length -and $Text[$Position] -eq '"'
}

# Copies code to the output until the end of the text, or (in a hole) until the "}" that closes the hole. The "}" stays
# at $State.Position. Strings, character literals, and comments are masked.
function Read-CSharpCode([hashtable] $State, [bool] $IsHole) {
  $text = $State.Text
  $length = $text.Length
  $depth = 0
  while ($State.Position -lt $length) {
    $position = $State.Position
    $current = $text[$position]
    $next = if ($position + 1 -lt $length) { $text[$position + 1] } else { [char]0 }
    if ($current -eq '/' -and $next -eq '/') {
      $end = $text.IndexOf("`n", $position); if ($end -lt 0) { $end = $length }
      Add-MaskedRange $State $position $end; $State.Position = $end
    } elseif ($current -eq '/' -and $next -eq '*') {
      $end = $text.IndexOf('*/', $position + 2); $end = if ($end -lt 0) { $length } else { $end + 2 }
      Add-MaskedRange $State $position $end; $State.Position = $end
    } elseif ($current -eq '"' -or (($current -eq '@' -or $current -eq '$') -and $next -in '"', '@', '$' -and
        (Test-StringPrefix $text $position))) {
      Read-CSharpString $State
    } elseif ($current -eq "'") {
      $end = $position + 1
      while ($end -lt $length -and $text[$end] -ne "'" -and $text[$end] -ne "`n") {
        if ($text[$end] -eq '\') { $end++ }
        $end++
      }
      $end = [Math]::Min($end + 1, $length)
      Add-MaskedRange $State $position $end; $State.Position = $end
    } else {
      if ($current -eq '{') { $depth++ }
      if ($current -eq '}') {
        if ($IsHole -and $depth -eq 0) { return }
        $depth--
      }
      [void]$State.Output.Append($current); $State.Position++
    }
  }
}

# Masks one string literal that starts at $State.Position. An interpolation hole is read as code.
function Read-CSharpString([hashtable] $State) {
  $text = $State.Text
  $length = $text.Length
  $start = $State.Position
  $quoteStart = $start
  while ($text[$quoteStart] -ne '"') { $quoteStart++ }
  $prefix = $text.Substring($start, $quoteStart - $start)
  $isVerbatim = $prefix.Contains('@')
  $dollars = ($prefix.ToCharArray() | Where-Object { $_ -eq '$' }).Count
  $quotes = 0
  while ($quoteStart + $quotes -lt $length -and $text[$quoteStart + $quotes] -eq '"') { $quotes++ }
  $isRaw = $quotes -ge 3
  $holeBraces = if ($isRaw) { [Math]::Max($dollars, 1) } else { 1 }
  $position = $quoteStart + $(if ($isRaw) { $quotes } else { 1 })
  $maskedFrom = $start
  while ($position -lt $length) {
    $current = $text[$position]
    if ($isRaw) {
      if ($current -eq '"' -and $text.Substring($position).StartsWith('"' * $quotes)) { $position += $quotes; break }
    } elseif ($current -eq '"') {
      if ($isVerbatim -and $position + 1 -lt $length -and $text[$position + 1] -eq '"') { $position += 2; continue }
      $position++; break
    } elseif ($current -eq '\' -and -not $isVerbatim) { $position += 2; continue }
    if ($dollars -gt 0 -and $current -eq '{') {
      $openAt = $position
      if ($isRaw) {
        $run = 0
        while ($position + $run -lt $length -and $text[$position + $run] -eq '{') { $run++ }
        if ($run -lt $holeBraces) { $position += $run; continue }
        $openAt = $position + $run - $holeBraces
      } elseif ($position + 1 -lt $length -and $text[$position + 1] -eq '{') {
        $position += 2
        continue
      }
      $holeStart = $openAt + $holeBraces
      Add-MaskedRange $State $maskedFrom $holeStart
      $State.Position = $holeStart
      Read-CSharpCode $State $true
      $maskedFrom = $State.Position
      $position = $State.Position + $holeBraces
      continue
    }
    $position++
  }
  $position = [Math]::Min($position, $length)
  Add-MaskedRange $State $maskedFrom $position
  $State.Position = $position
}

# One typed line for each element access with a named argument (list[index: 0], map[key: "x"], { [key: "z"] = 4 }) in
# the given C# files (repository-relative) outside Migrations/:
#   <repo path>(<line>,<column>): error NAMED_INDEX: <message>
# The column is the one of the argument name. The JetBrains cleanup never writes such a name for a single argument
# (resharper_arguments_skip_single), so a name comes from a hand edit. Attribute targets ([return: ...], [assembly: ...]),
# strings, and comments never match. White space and line breaks may sit between the receiver and "[".
# A "[" after return, yield, in, is, case, or await starts a collection expression, not an index.
function Get-NamedIndexArguments([string] $Repo, [string[]] $Files) {
  $attributeTargets = 'assembly|module|return|field|property|param|method|type|event'
  $pattern = "(?:(?<=[\w)\]?!]\s*)|(?<=[{,]\s*))\[(?!(?:$attributeTargets):)\s*(?<name>[A-Za-z_]\w*):\s"
  $collectionKeywords = 'return', 'yield', 'in', 'is', 'case', 'await'
  foreach ($file in @($Files | Where-Object { $_ -like '*.cs' -and $_ -notmatch '(^|/)Migrations/' })) {
    $path = Join-Path $Repo $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $masked = Get-MaskedCSharp ([IO.File]::ReadAllText($path))
    foreach ($match in [regex]::Matches($masked, $pattern)) {
      $group = $match.Groups['name']
      $before = $masked.Substring(0, $group.Index)
      $word = [regex]::Match($before.Substring(0, $match.Index).TrimEnd(), '\w+$').Value
      if ($word -in $collectionKeywords) { continue }
      $line = ($before.Split("`n").Count)
      $column = $group.Index - $before.LastIndexOf("`n")
      "$file($line,$column): error NAMED_INDEX: the index access names its argument; write list[0], not list[index: 0]."
    }
  }
}
