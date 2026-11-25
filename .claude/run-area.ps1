#Requires -Version 7
# run-area.ps1 - the full run of one test area for the executor (.claude/agents/executor.md, Execution). It formats the
# changed files of the area first, so the tree of the Full run line is the tree that check-handoff.ps1 sees after its
# own format step (a format change after a full run made the run stale and cost a second run).
# Usage: pwsh .claude/run-area.ps1 -Area backend|frontend [-Repo <path>] [-Base <ref>] [-MainBranch <name>]
#        [-FormatScript <path>]
# Defaults: -Repo is the checkout that holds this script file; -Base is the merge base of HEAD and -MainBranch
# (develop); -FormatScript is format-changed.ps1 next to this file.
# Lock: one full run per area at a time. Before step 1 the script creates run-<area>.lock (an exclusive create, so 2
# runs that start together cannot both pass) with its process ID in the claude-runs folder of the git state, and it deletes the file when the run ends (also after RUN FAILED and an
# exception). When the lock file exists and its process still runs, the script prints "RUN FAILED <area>: another run of
# <area> is active (pid <id>)", leaves the lock, and exits 1 at once. A lock of a process that no longer runs is replaced.
# Steps, in order; the first failure ends the run:
# 1. The format script on the existing files of the area that changed since the base (tracked and untracked). Its notes
#    are printed as "Formatter: <note>".
# 2. The line length check of the changed C# files outside Migrations/ (hooks/checkout-helpers.ps1, Get-LongLines).
# 3. The tree ID of the area (tree-id.ps1 -Area).
# 4. The full commands of the area (CLAUDE.md, Build and Test). Backend: the build, the unit and architecture command,
#    and the integration and API command (only when the unit command passes). Frontend: lint, ng build, and ng test in
#    frontend/, plus lint and test in keycloak-theme/ when keycloak-theme/ changed since the base. Each command writes
#    its output to a log file in the git directory (claude-runs/) and prints one line with its result and the log path,
#    then the names of its failing tests.
# 5. The area tree again: a change during the run fails it.
# Last line: "Full run <area>: tree <id>; <commands> -> <passed counts> passed, 0 failed" (exit code 0), or
# "RUN FAILED <area>: <reason>" (exit code 1). The executor copies the Full run line into plan.md with the Edit tool;
# this script never writes plan.md (agent-workflow.md, Handoff Files). It runs longer than 180 seconds, so the executor
# starts it in the background and waits for the last line with the Monitor tool.
param(
  [Parameter(Mandatory = $true)][ValidateSet('backend', 'frontend')][string]$Area,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$Base,
  [string]$MainBranch = 'develop',
  [string]$FormatScript = (Join-Path $PSScriptRoot 'format-changed.ps1')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$FrontendPaths = @('frontend', 'keycloak-theme')
$ToolPaths = @('.claude', 'CLAUDE.md')
$UnitModules = 'backend/tests/**/bin/Debug/net10.0/HealthyFac.*.UnitTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ArchitectureTests.dll'
$IntegrationModules = 'backend/tests/**/bin/Debug/net10.0/HealthyFac.*.IntegrationTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ApiTests.dll'
$IntegrationParallelModules = 4

$script:OwnedLock = $null

function Remove-RunLock {
  if ($script:OwnedLock) {
    Remove-Item -LiteralPath $script:OwnedLock -Force -ErrorAction SilentlyContinue
    $script:OwnedLock = $null
  }
}

function Stop-Run([string] $reason) {
  Remove-RunLock
  "RUN FAILED ${Area}: $reason"
  exit 1
}

# Takes the lock of the area, or ends the run when a live process holds it. The lock file is created with an
# exclusive create, so 2 runs that start together cannot both own it. A lock file that a run holds open, or that
# is still empty, counts as live. A stale lock (a process that no longer runs) is deleted and created again.
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
      $stream.Dispose()
      $stream = $null
      $script:OwnedLock = $path
      return
    } catch [IO.IOException] {
      if ($stream) { $stream.Dispose() }
      if (-not (Test-Path -LiteralPath $path)) { continue }
      $text = $null
      try { $text = (Get-Content -LiteralPath $path -ErrorAction Stop | Select-Object -First 1) -as [string] } catch { $text = $null }
      $holder = 0
      $isNumber = [int]::TryParse($text, [ref]$holder)
      if (-not $isNumber) {
        $isFresh = ((Get-Date) - (Get-Item -LiteralPath $path -ErrorAction SilentlyContinue).LastWriteTime).TotalSeconds -lt 10
        if ($isFresh -or $null -eq $text) { Stop-Lock 'unknown' }
      } elseif ($holder -gt 0 -and $holder -ne $PID -and (Get-Process -Id $holder -ErrorAction SilentlyContinue)) {
        Stop-Lock "$holder"
      }
      Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    }
  }
  Stop-Lock 'unknown'
}

function Stop-Lock([string] $holder) {
  "RUN FAILED ${Area}: another run of $Area is active (pid $holder)"
  exit 1
}

function Test-InArea([string] $path) {
  $isFrontend = @($FrontendPaths | Where-Object { $path -eq $_ -or $path -like "$_/*" }).Count -gt 0
  if ($Area -eq 'frontend') { return $isFrontend }
  $isTool = @($ToolPaths | Where-Object { $path -eq $_ -or $path -like "$_/*" }).Count -gt 0
  return -not $isFrontend -and -not $isTool
}

# The passed count of a test command: the "succeeded:" line of the Microsoft Testing Platform summary, or the
# "Tests <n> passed" line of vitest. Nothing for a command without tests.
function Get-PassedCount([string[]] $lines) {
  $succeeded = @($lines | Select-String -Pattern '^\s*succeeded:\s*(\d+)' | Select-Object -Last 1)
  if ($succeeded) { return [int]$succeeded[0].Matches[0].Groups[1].Value }
  $vitest = @($lines | Select-String -Pattern '^\s*Tests\s+(?:\d+ failed \| )?(\d+) passed' | Select-Object -Last 1)
  if ($vitest) { return [int]$vitest[0].Matches[0].Groups[1].Value }
}

function Get-FailedTests([string[]] $lines) {
  @($lines | Where-Object { $_ -match '^\s*failed\s+\S' -and $_ -notmatch '^\s*failed:' -or $_ -match '^\s*(FAIL|×)\s' } |
    ForEach-Object { $_.Trim() })
}

# The steps of the header. Each failure prints its RUN FAILED line and exits; a throw (a tree ID, the format script,
# a missing tool) reaches the catch below, so the last line is always a Full run or a RUN FAILED line.
function Invoke-AreaRun {
  Enter-RunLock
  if (-not $Base) {
    $Base = git -C $Repo merge-base HEAD $MainBranch 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $Base) { Stop-Run "no merge base of HEAD and $MainBranch. Pass -Base <ref>." }
    $Base = $Base.Trim()
  }

  $changed = @(@((Get-GitLines $Repo @('diff', '--name-only', '--no-renames', $Base)).Lines) +
    @((Get-GitLines $Repo @('ls-files', '--others', '--exclude-standard')).Lines) |
      Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique |
      Where-Object { Test-InArea $_ })
  $existing = @($changed | Where-Object { Test-Path -LiteralPath (Join-Path $Repo $_) -PathType Leaf })

  if ($existing.Count -gt 0 -and $FormatScript) {
    foreach ($note in @(& $FormatScript -Repo $Repo -Files $existing)) { "Formatter: $note" }
  }

  $longLines = @(Get-LongLines -Repo $Repo -Files $existing)
  foreach ($line in $longLines) { "$($line.File):$($line.Line) has $($line.Length) characters; max_line_length is $($line.Limit)." }
  if ($longLines.Count -gt 0) { Stop-Run "$($longLines.Count) long lines" }

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
  $number = 0
  foreach ($command in $commands) {
    $number++
    $log = Join-Path $logFolder "$Area-$stamp-$number.log"
    Push-Location (Join-Path $Repo $command.Folder)
    try {
      & $command.Tool @($command.Arguments) *> $log
      $exitCode = $LASTEXITCODE
    }
    finally { Pop-Location }
    $lines = @(Get-Content -LiteralPath $log -ErrorAction SilentlyContinue | ForEach-Object { $_ -replace '\x1b\[[0-9;]*m', '' })
    $count = Get-PassedCount $lines
    $countText = if ($null -ne $count) { "$count passed" } else { 'no test count' }
    $result = if ($exitCode -eq 0) { 'passed' } else { 'failed' }
    "$($command.Label): $result (exit $exitCode) -> $countText; log $log"
    if ($exitCode -ne 0) {
      Get-FailedTests $lines
      Stop-Run "$($command.Label) failed"
    }
    if ($null -ne $count) { $passed.Add($count) }
  }

  $treeAfter = Get-TreeId -Repo $Repo -Area $Area
  if ($treeAfter -ne $treeBefore) { Stop-Run 'the tree changed during the run' }

  Remove-RunLock
  "Full run ${Area}: tree $treeBefore; $(@($commands | ForEach-Object { $_.Label }) -join ', ') -> $($passed -join ' and ') passed, 0 failed"
}

try { Invoke-AreaRun }
catch {
  Remove-RunLock
  "RUN FAILED ${Area}: $($_.Exception.Message)"
  exit 1
}
exit 0
