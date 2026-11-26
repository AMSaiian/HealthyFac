#Requires -Version 7
# Runs the tests that a change affects, or the tests of named features, or one test project.
# Usage: pwsh .claude/run-tests.ps1 -Area <backend|frontend|tooling> [-Kind <unit|integration|all>]
#          (-Feature <names> | -Changed | -Project <path> [-Filter <filter>]) [-Base <ref>] [-Repo <path>]
#        pwsh .claude/run-tests.ps1 -ShowResults [-Repo <path>]
#
# Backend and frontend runs format first, like run-area.ps1: the format script (format-changed.ps1) runs on the existing
# files of the area that changed since the base, then the build and the tests, then the tree ID. After a passing backend run
# the script writes the Stop hook state (Write-HookState), so the hook formats and builds nothing; the frontend run writes
# none, because it runs no lint.
#
# Features: test-features.json at the repo root lists the features with the backend and frontend globs of their code.
# -Changed selects the features whose globs match a path that changed since the base (git diff plus untracked files).
# A changed path of the area that matches no glob selects every feature. In the frontend, a path under
# frontend/src/app/shared/ or a file directly in frontend/src/app/ selects every feature too.
#
# Backend: the quiet agent build runs first. After a failed build, the script prints the SARIF diagnostics. Then:
# - unit: the unit and architecture test modules, no filter.
# - integration: the integration and API test modules (4 projects at a time), one --filter-trait "Feature=<name>" for
#   each selected feature (none when every feature is selected).
# - all (default): unit first, integration only when unit passes.
# - -Project: dotnet test --project <path> --no-build, with --filter <filter> when given.
# Frontend: npx ng test --watch=false in frontend/, with one --include <folder> for each frontend glob of the selected
# features and HF_TEST_TAGS set to the names joined with " || " (the Vitest tag expression). No include and no tag
# filter when every feature is selected. Selected features without a frontend glob start no frontend command. When a changed
# path starts with keycloak-theme/, ng test is followed by npm run lint and npm test in keycloak-theme/ (labels keycloak-theme lint
# and keycloak-theme test, as in run-area.ps1); a failure prints RUN FAILED frontend: <label> failed.
#
# Tooling: -Area tooling -Changed runs the script test suites that the changed tool files select (Get-ToolSuites), once each,
# at the same time. Last line: "Affected run tooling: tree <id>; suites <names> -> passed" or "RUN FAILED tooling: <suite> - <first FAIL line>".
#
# Results: each run stores its result (pass or fail) in the git directory (Write-RunResult, result-<area>-affected.json).
# -ShowResults prints one line for each stored result of the checkout (run-area.ps1 stores "full" results too):
# "<kind> <area>: tree <id>; <status>; <current|stale>" (current: the tree equals the tree of the area now).
#
# Each command writes its output to a log file in the git directory (claude-runs/) and prints one result line, then
# the names of its failing tests. The last line is "Affected run <area>: tree <id>; features <names>; <commands> ->
# <counts> passed, 0 failed" (exit code 0), or "RUN FAILED <area>: <reason>" (exit code 1). The tree ID is the one of
# the area (tree-id.ps1 -Area); a change of the area during the run fails it.
[CmdletBinding()]
param(
  [ValidateSet('backend', 'frontend', 'tooling')][string]$Area,
  [ValidateSet('unit', 'integration', 'all')][string]$Kind = 'all',
  [string[]]$Feature,
  [switch]$Changed,
  [string]$Project,
  [switch]$ShowResults,
  [string]$Filter,
  [string]$Base,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$MainBranch = 'develop',
  [string]$FormatScript = (Join-Path $PSScriptRoot 'format-changed.ps1')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$FeatureFileName = 'test-features.json'

function Stop-Run([string] $reason) {
  $line = "RUN FAILED ${Area}: $reason"
  $line
  $tree = try { Get-TreeId -Repo $Repo -Area $Area } catch { '' }
  Write-RunResult $Repo 'affected' $Area $tree 'fail' $line
  exit 1
}

# The features of test-features.json as objects with Name, Backend, and Frontend (arrays of globs).
function Read-Features {
  $path = Join-Path $Repo $FeatureFileName
  if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Stop-Run "no $FeatureFileName at the repo root" }
  @((Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).features | ForEach-Object {
      [pscustomobject]@{ Name = $_.name; Backend = @($_.backend); Frontend = @($_.frontend) }
    })
}

# The names of the features that the changed paths of the area select, in the order of the feature file.
function Get-ChangedFeatureNames([object[]] $features, [string[]] $paths) {
  $selected = [Collections.Generic.HashSet[string]]::new()
  foreach ($path in $paths) {
    $isEveryFeature = $Area -eq 'frontend' -and ($path -like 'frontend/src/app/shared/*' -or $path -match '^frontend/src/app/[^/]+$')
    $matched = @($features | Where-Object {
        $globs = if ($Area -eq 'frontend') { $_.Frontend } else { $_.Backend }
        @($globs | Where-Object { $path -match (ConvertTo-GlobRegex $_) }).Count -gt 0
      })
    if ($isEveryFeature -or $matched.Count -eq 0) { return @($features | ForEach-Object { $_.Name }) }
    foreach ($feature in $matched) { [void]$selected.Add($feature.Name) }
  }
  @($features | Where-Object { $selected.Contains($_.Name) } | ForEach-Object { $_.Name })
}

# Stores the pass result and prints the last line.
function Complete-Run([string] $tree, [string] $line) {
  Write-RunResult $Repo 'affected' $Area $tree 'pass' $line
  $line
}

function Invoke-TestCommand([string] $label, [string] $folder, [string] $tool, [string[]] $arguments) {
  $script:CommandNumber++
  $log = Join-Path $script:LogFolder "$Area-affected-$script:Stamp-$script:CommandNumber.log"
  $run = Invoke-RunCommand -Repo $Repo -Label $label -Folder $folder -Tool $tool -Arguments $arguments -Log $log
  $run.Lines
  if ($label -eq 'build') { $script:BuildLines = $run.Output }
  if ($run.ExitCode -ne 0) { Stop-Run "$label failed" }
  if ($null -ne $run.Count) { $script:Passed.Add($run.Count) }
  $script:Labels.Add($label)
}

function Invoke-TestRun {
  if (-not $Base) {
    $Base = Get-DefaultBase $Repo $MainBranch
    if (-not $Base) { Stop-Run "no merge base of HEAD and $MainBranch. Pass -Base <ref>." }
  }
  $modes = @(($null -ne $Feature -and @($Feature).Count -gt 0), [bool]$Changed, [bool]$Project) | Where-Object { $_ }
  if ($modes.Count -ne 1) { Stop-Run 'pass exactly one of -Feature, -Changed, or -Project' }
  if ($Project -and $Area -ne 'backend') { Stop-Run '-Project works only for the backend area' }
  if ($Area -eq 'tooling') {
    if (-not $Changed) { Stop-Run '-Area tooling works only with -Changed' }
    $run = Invoke-ToolingRun -Repo $Repo -Area $Area -Paths (Get-AreaChangedPaths $Repo $Base $Area) -Mode affected
    $run.Lines
    if ($run.Failure) { Stop-Run $run.Failure }
    Complete-Run $run.Tree $run.LastLine
    return
  }

  $features = Read-Features
  $changedPaths = Get-AreaChangedPaths $Repo $Base $Area
  $allNames = @($features | ForEach-Object { $_.Name })
  $names = if ($Project) { @() }
  elseif ($Changed) { Get-ChangedFeatureNames $features $changedPaths }
  else {
    $requested = @($Feature | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $unknown = @($requested | Where-Object { $_ -notin $allNames })
    if ($unknown.Count -gt 0) { Stop-Run "unknown feature $($unknown -join ', ')" }
    $requested
  }
  $isEveryFeature = $names.Count -eq $allNames.Count
  $featureText = if ($Project) { "project $Project" } elseif ($isEveryFeature) { 'all' } elseif ($names.Count -eq 0) { 'none' } else { $names -join ', ' }

  $existing = @($changedPaths | Where-Object { Test-Path -LiteralPath (Join-Path $Repo $_) -PathType Leaf })
  if ($existing.Count -gt 0 -and $FormatScript) {
    foreach ($note in @(& $FormatScript -Repo $Repo -Files $existing)) { "Formatter: $note" }
  }

  $treeBefore = Get-TreeId -Repo $Repo -Area $Area
  $script:LogFolder = Get-GitStatePath $Repo 'claude-runs'
  New-Item -ItemType Directory -Force $script:LogFolder | Out-Null
  $script:Stamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
  $script:CommandNumber = 0
  $script:Passed = [Collections.Generic.List[int]]::new()
  $script:Labels = [Collections.Generic.List[string]]::new()
  $script:BuildLines = @()

  if ($Area -eq 'backend') {
    if ($names.Count -eq 0 -and -not $Project) { $featureText = 'none' }
    Invoke-TestCommand 'build' '.' 'dotnet' @('build', 'backend/HealthyFac.slnx', '-v:q', '-nologo', '-clp:NoSummary')
    if ($Project) {
      $arguments = @('test', '--project', $Project, '--no-build') + $(if ($Filter) { @('--filter', $Filter) } else { @() })
      Invoke-TestCommand 'project tests' '.' 'dotnet' $arguments
    } else {
      if ($Kind -in 'unit', 'all') {
        Invoke-TestCommand 'unit+architecture command' '.' 'dotnet' @('test', '--test-modules', $script:UnitModules)
      }
      if ($Kind -in 'integration', 'all') {
        $traitArguments = if ($isEveryFeature) { @() } else { @($names | ForEach-Object { @('--filter-trait', "Feature=$_") }) }
        $zeroTestArguments = if ($traitArguments.Count -gt 0) { @('--ignore-exit-code', '8') } else { @() }
        Invoke-TestCommand 'integration+API command' '.' 'dotnet' (@('test', '--test-modules', $script:IntegrationModules,
            '--max-parallel-test-modules', "$script:IntegrationParallelModules") + $traitArguments + $zeroTestArguments)
      }
    }
  } else {
    $selected = @($features | Where-Object { $_.Name -in $names })
    $includes = @($selected | ForEach-Object { $_.Frontend } | ForEach-Object { ($_ -replace '^frontend/', '') -replace '/\*\*$', '' } |
        Select-Object -Unique)
    if (-not $isEveryFeature -and $includes.Count -eq 0) {
      $featureText = if ($names.Count -eq 0) { 'none' } else { $names -join ', ' }
      $script:Labels.Add('no frontend tests')
    } else {
      $arguments = @('ng', 'test', '--watch=false')
      if (-not $isEveryFeature) {
        foreach ($include in $includes) { $arguments += @('--include', $include) }
        $env:HF_TEST_TAGS = $names -join ' || '
      }
      try { Invoke-TestCommand 'ng test' 'frontend' 'npx' $arguments }
      finally { Remove-Item Env:HF_TEST_TAGS -ErrorAction SilentlyContinue }
    }
    if (@($changedPaths | Where-Object { $_ -like 'keycloak-theme/*' }).Count -gt 0) {
      Invoke-TestCommand 'keycloak-theme lint' 'keycloak-theme' 'npm' @('run', 'lint')
      Invoke-TestCommand 'keycloak-theme test' 'keycloak-theme' 'npm' @('test')
    }
  }

  $treeAfter = Get-TreeId -Repo $Repo -Area $Area
  if ($treeAfter -ne $treeBefore) { Stop-Run 'the tree changed during the run' }

  if ($Area -eq 'backend') { Write-HookState $Repo $Area -BuildOutput $script:BuildLines }
  $countText = if ($script:Passed.Count -gt 0) { $script:Passed -join ' and ' } else { '0' }
  Complete-Run $treeBefore "Affected run ${Area}: tree $treeBefore; features $featureText; $($script:Labels -join ', ') -> $countText passed, 0 failed"
}

if ($ShowResults) {
  foreach ($result in Get-RunResults $Repo) {
    $isCurrent = $result.tree -eq (Get-TreeId -Repo $Repo -Area $result.area)
    "$($result.kind) $($result.area): tree $($result.tree); $($result.status); $(if ($isCurrent) { 'current' } else { 'stale' })"
  }
  exit 0
}
if (-not $Area) { "RUN FAILED: pass -Area or -ShowResults"; exit 1 }

try { Invoke-TestRun }
catch {
  $line = "RUN FAILED ${Area}: $($_.Exception.Message)"
  $line
  $tree = try { Get-TreeId -Repo $Repo -Area $Area } catch { '' }
  Write-RunResult $Repo 'affected' $Area $tree 'fail' $line
  exit 1
}
exit 0
