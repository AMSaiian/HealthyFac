#Requires -Version 7
# run-area.ps1 - the full run of one test area for the executor (.claude/agents/executor.md, Execution). It formats the
# changed files of the area first, so the tree of the run result is the tree that check-handoff.ps1 reads (it formats
# nothing, Step 73).
# Usage: pwsh .claude/run-area.ps1 -Area backend|frontend|tooling [-Repo <path>] [-Base <ref>] [-MainBranch <name>]
#        [-FormatScript <path>]
# Defaults: -Repo is the checkout that holds this script file; -Base is the merge base of HEAD and origin/<MainBranch>
# (develop; the local branch when origin has none, Get-DefaultBase); -FormatScript is format-changed.ps1 next to this file.
# Lock: one full run per area at a time. Before step 1 the script creates run-<area>.lock (an exclusive create, so 2
# runs that start together cannot both pass) with its process ID in the claude-runs folder of the git state. The script
# keeps the file open for the whole run (read share only, so no other process can delete or write it), and it closes
# and deletes the file when the run ends (also after RUN FAILED and an exception). A lock file that no run holds open
# is stale: the next run deletes it. When a run holds the file open, the next run prints "RUN FAILED <area>: another
# run of <area> is active (pid <id>)", leaves the lock, and exits 1 at once.
# Area tooling (.claude/, CLAUDE.md, scripts/): no format step, no named index check, no build. It runs each script test
# suite whose pattern matches a path that changed since the base, once (Get-ToolSuites in checkout-helpers.ps1; the suites run
# at the same time, each with its own log). Last line: "Full run tooling: tree <id>; suites <names> -> passed" (or
# "suites none" when no pattern matches), or "RUN FAILED tooling: <suite> - <first FAIL line>". The lock applies to it.
# Steps of the other areas, in order; the first failure ends the run:
# 1. The format script on the existing files of the area that changed since the base (tracked and untracked). Its notes
#    are printed as "Formatter: <note>".
# 2. The named index argument check of the changed C# files outside Migrations/ (hooks/checkout-helpers.ps1, Get-NamedIndexArguments).
# 3. The tree ID of the area (tree-id.ps1 -Area).
# 4. The full commands of the area (CLAUDE.md, Build and Test). Backend: the build, the unit and architecture command,
#    and the integration and API command (only when the unit command passes). Frontend: lint, ng build, and ng test in
#    frontend/, plus lint and test in keycloak-theme/ when keycloak-theme/ changed since the base. Each command writes
#    its output to a log file in the git directory (claude-runs/) and prints one line with its result and the log path,
#    then the names of its failing tests. A failed backend build prints its diagnostics first as typed lines from the
#    SARIF files under backend/ (Get-SarifDiagnostics), errors first.
# 5. The area tree again: a change during the run fails it.
# After a passing run, the hook state of the area (Write-HookState): the Stop hook then finds the verified tree and formats and
# builds nothing. A backend run with a SARIF warning in a changed file prints "Hook check: <line>" and writes no state.
# Last line: "Full run <area>: tree <id>; <commands> -> <passed counts> passed, 0 failed" (exit code 0), or
# "RUN FAILED <area>: <reason>" (exit code 1). The script stores its result, pass or fail (Write-RunResult,
# result-<area>-full.json in the git directory), and check-handoff.ps1 reads it; the line goes into no plan.md. It
# runs longer than 180 seconds, so the executor
# runs it in the foreground with timeout 600000 (never in the background; a hook denies that). When the harness moves the
# run to the background, the executor waits with one foreground loop on the output file until the last line shows.
param(
  [Parameter(Mandatory = $true)][ValidateSet('backend', 'frontend', 'tooling')][string]$Area,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$Base,
  [string]$MainBranch = 'develop',
  [string]$FormatScript = (Join-Path $PSScriptRoot 'format-changed.ps1')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$UnitModules = 'backend/tests/**/bin/Debug/net10.0/HealthyFac.*.UnitTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ArchitectureTests.dll'
$IntegrationModules = 'backend/tests/**/bin/Debug/net10.0/HealthyFac.*.IntegrationTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ApiTests.dll'
$IntegrationParallelModules = 4

$script:OwnedLock = $null
$script:LockStream = $null

function Remove-RunLock {
  if ($script:LockStream) {
    $script:LockStream.Dispose()
    $script:LockStream = $null
  }
  if ($script:OwnedLock) {
    Remove-Item -LiteralPath $script:OwnedLock -Force -ErrorAction SilentlyContinue
    $script:OwnedLock = $null
  }
}

# Stores the result of the run (Write-RunResult), with the tree of the area now when it can be read.
function Save-Result([string] $status, [string] $line) {
  $tree = try { Get-TreeId -Repo $Repo -Area $Area } catch { '' }
  Write-RunResult $Repo 'full' $Area $tree $status $line
}

function Stop-Run([string] $reason) {
  Remove-RunLock
  $line = "RUN FAILED ${Area}: $reason"
  $line
  Save-Result 'fail' $line
  exit 1
}

# Ends a passing run: it releases the lock, stores the pass result with the tree of the Full run line, and prints the line.
function Complete-Run([string] $tree, [string] $line) {
  Remove-RunLock
  Write-RunResult $Repo 'full' $Area $tree 'pass' $line
  $line
}

# Takes the lock of the area, or ends the run when another run holds it. The lock file is created with an exclusive
# create and stays open (read share only) until the run ends, so a delete of it fails while its run lives. When the
# create fails, the script tries to delete the file: a delete that fails means that a live run holds it, and a delete
# that works removed a stale file, so the next create attempt decides between runs that start together.
function Enter-RunLock {
  $folder = Get-GitStatePath $Repo 'claude-runs'
  New-Item -ItemType Directory -Force $folder | Out-Null
  $path = Join-Path $folder "run-$Area.lock"
  for ($attempt = 0; $attempt -lt 5; $attempt++) {
    $stream = $null
    try {
      $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
      $bytes = [Text.Encoding]::ASCII.GetBytes("$PID")
      $stream.Write($bytes, 0, $bytes.Length)
      $stream.Flush()
      $script:LockStream = $stream
      $script:OwnedLock = $path
      return
    } catch [IO.IOException] {
      if ($stream) { $stream.Dispose() }
      if (-not (Test-Path -LiteralPath $path)) { continue }
      try {
        Remove-Item -LiteralPath $path -Force -ErrorAction Stop
      } catch {
        $text = $null
        try { $text = (Get-Content -LiteralPath $path -ErrorAction Stop | Select-Object -First 1) -as [string] } catch { $text = $null }
        Stop-Lock $(if ([string]::IsNullOrWhiteSpace($text)) { 'unknown' } else { $text.Trim() })
      }
    }
  }
  Stop-Lock 'unknown'
}

function Stop-Lock([string] $holder) {
  "RUN FAILED ${Area}: another run of $Area is active (pid $holder)"
  exit 1
}

# The tooling area: the suites that the changed paths select (Get-ToolSuites) run at the same time, once each. The tree is
# read before and after, like for the other areas.
function Invoke-ToolingRun([string[]] $changed) {
  $treeBefore = Get-TreeId -Repo $Repo -Area $Area
  $suites = @(Get-ToolSuites $Repo $changed)
  $run = Invoke-ToolSuites $Repo $suites $Area
  $run.Lines
  if ($run.Failure) { Stop-Run $run.Failure }

  $treeAfter = Get-TreeId -Repo $Repo -Area $Area
  if ($treeAfter -ne $treeBefore) { Stop-Run 'the tree changed during the run' }
  $names = if ($suites.Count -gt 0) { @($suites | ForEach-Object { Split-Path $_ -Leaf }) -join ', ' } else { 'none' }
  Complete-Run $treeBefore "Full run ${Area}: tree $treeBefore; suites $names -> passed"
}
# The steps of the header. Each failure prints its RUN FAILED line and exits; a throw (a tree ID, the format script,
# a missing tool) reaches the catch below, so the last line is always a Full run or a RUN FAILED line.
function Invoke-AreaRun {
  Enter-RunLock
  if (-not $Base) {
    $Base = Get-DefaultBase $Repo $MainBranch
    if (-not $Base) { Stop-Run "no merge base of HEAD and $MainBranch. Pass -Base <ref>." }
  }

  $changed = @(@((Get-GitLines $Repo @('diff', '--name-only', '--no-renames', $Base)).Lines) +
    @((Get-GitLines $Repo @('ls-files', '--others', '--exclude-standard')).Lines) |
      Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique |
      Where-Object { Test-PathInArea $_ $Area })
  $existing = @($changed | Where-Object { Test-Path -LiteralPath (Join-Path $Repo $_) -PathType Leaf })

  if ($Area -eq 'tooling') {
    Invoke-ToolingRun $changed
    return
  }

  if ($existing.Count -gt 0 -and $FormatScript) {
    foreach ($note in @(& $FormatScript -Repo $Repo -Files $existing)) { "Formatter: $note" }
  }

  $namedIndexes = @(Get-NamedIndexArguments -Repo $Repo -Files $existing)
  foreach ($line in $namedIndexes) { $line }
  if ($namedIndexes.Count -gt 0) { Stop-Run "$($namedIndexes.Count) named index arguments" }

  $treeBefore = Get-TreeId -Repo $Repo -Area $Area

  $commands = [Collections.Generic.List[object]]::new()
  if ($Area -eq 'backend') {
    $commands.Add(@{ Label = 'build'; Folder = '.'; Tool = 'dotnet'; Arguments = @('build', 'backend/HealthyFac.slnx') })
    $commands.Add(@{ Label = 'unit+architecture command'; Folder = '.'; Tool = 'dotnet'; Arguments = @('test', '--test-modules', $UnitModules) })
    $commands.Add(@{ Label = 'integration+API command'; Folder = '.'; Tool = 'dotnet'
        Arguments = @('test', '--test-modules', $IntegrationModules, '--max-parallel-test-modules', "$IntegrationParallelModules") })
  } else {
    $commands.Add(@{ Label = 'lint'; Folder = 'frontend'; Tool = 'npm'; Arguments = @('run', 'lint') })
    $commands.Add(@{ Label = 'ng build'; Folder = 'frontend'; Tool = 'npx'; Arguments = @('ng', 'build') })
    $commands.Add(@{ Label = 'ng test'; Folder = 'frontend'; Tool = 'npx'; Arguments = @('ng', 'test', '--watch=false') })
    if (@($changed | Where-Object { $_ -like 'keycloak-theme/*' }).Count -gt 0) {
      $commands.Add(@{ Label = 'keycloak-theme lint'; Folder = 'keycloak-theme'; Tool = 'npm'; Arguments = @('run', 'lint') })
      $commands.Add(@{ Label = 'keycloak-theme test'; Folder = 'keycloak-theme'; Tool = 'npm'; Arguments = @('test') })
    }
  }

  $logFolder = Get-GitStatePath $Repo 'claude-runs'
  New-Item -ItemType Directory -Force $logFolder | Out-Null
  $stamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
  $passed = [Collections.Generic.List[int]]::new()
  $buildLines = @()
  $number = 0
  foreach ($command in $commands) {
    $number++
    $log = Join-Path $logFolder "$Area-$stamp-$number.log"
    $commandStartedAt = [DateTime]::UtcNow
    Push-Location (Join-Path $Repo $command.Folder)
    try {
      & $command.Tool @($command.Arguments) *> $log
      $exitCode = $LASTEXITCODE
    }
    finally { Pop-Location }
    $lines = @(Get-Content -LiteralPath $log -ErrorAction SilentlyContinue | ForEach-Object { $_ -replace '\x1b\[[0-9;]*m', '' })
    $count = Get-PassedCount $lines
    if ($command.Label -eq 'build') { $buildLines = $lines }
    $countText = if ($null -ne $count) { "$count passed" } else { 'no test count' }
    $result = if ($exitCode -eq 0) { 'passed' } else { 'failed' }
    "$($command.Label): $result (exit $exitCode) -> $countText; log $log"
    if ($exitCode -ne 0) {
      if ($command.Label -eq 'build') {
        $diagnostics = @(Get-SarifDiagnostics -Repo $Repo -Root (Join-Path $Repo 'backend') -BuildOutput $lines -Since $commandStartedAt)
        $diagnostics | Where-Object { $_ -match ': error ' }
        $diagnostics | Where-Object { $_ -notmatch ': error ' }
      }
      Get-FailedTests $lines
      Stop-Run "$($command.Label) failed"
    }
    if ($null -ne $count) { $passed.Add($count) }
  }

  $treeAfter = Get-TreeId -Repo $Repo -Area $Area
  if ($treeAfter -ne $treeBefore) { Stop-Run 'the tree changed during the run' }

  Write-HookState $Repo $Area -BuildOutput $buildLines -RanKeycloakTheme:(@($commands | Where-Object { $_.Folder -eq 'keycloak-theme' }).Count -gt 0)
  Complete-Run $treeBefore "Full run ${Area}: tree $treeBefore; $(@($commands | ForEach-Object { $_.Label }) -join ', ') -> $($passed -join ' and ') passed, 0 failed"
}

try { Invoke-AreaRun }
catch {
  Remove-RunLock
  $line = "RUN FAILED ${Area}: $($_.Exception.Message)"
  $line
  Save-Result 'fail' $line
  exit 1
}
exit 0
