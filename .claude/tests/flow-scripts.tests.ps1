#Requires -Version 7
# Deterministic check of the flow scripts: tree-id.ps1, check-plan.ps1, check-handoff.ps1, and
# check-review-routing.ps1. It builds throw-away git repositories and feature folders in a temp folder and runs the
# real scripts. It uses no test framework (same form as .claude/hooks/tests/hook-scope.tests.ps1).
#
# Cases:
# - tree-id: the same ID twice without a change; a new ID after an edit of a tracked file, a new untracked file, and a
#   deletion; an ignored file changes nothing; the index, the status, and HEAD are the same before and after; it works
#   in a git worktree.
# - check-plan: a sample plan with cycles 1 and 2 passes; each of the 4 problems fails alone with its line; a problem
#   in cycle 1 does not fail a check of cycle 2.
# - check-handoff: a clean case passes; each of the 4 problems fails alone with its line; each TDD step is checked on its own.
# - check-review-routing: a checkpoint section after the final verdict does not change the route.
# Red: run it with -ScriptsDir <folder of empty stubs> to see each case fail first.
# Run:  pwsh .claude/tests/flow-scripts.tests.ps1 [-ScriptsDir <folder>]
# It exits 1 when a check fails.
param([string]$ScriptsDir = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent)))

$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) "hf-flow-tests-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
New-Item -ItemType Directory -Force $work | Out-Null
$failures = [Collections.Generic.List[string]]::new()

function Assert-That([bool] $condition, [string] $message) {
  if ($condition) { Write-Host "  ok   $message" } else {
    Write-Host "  FAIL $message" -ForegroundColor Red
    $failures.Add($message)
  }
}

function Invoke-Script([string] $name, [string[]] $arguments) {
  $output = & pwsh -NoProfile -File (Join-Path $ScriptsDir $name) @arguments 2>&1
  return [pscustomobject]@{ Output = @($output | ForEach-Object { "$_" }); Exit = $LASTEXITCODE }
}

function New-Repo([string] $name) {
  $dir = Join-Path $work $name
  New-Item -ItemType Directory -Force $dir | Out-Null
  git -C $dir init -q -b main
  git -C $dir config user.email 'test@example.com'
  git -C $dir config user.name 'Test'
  git -C $dir config core.autocrlf false
  return $dir
}

function Save-Commit([string] $dir, [string] $message) {
  git -C $dir add -A
  git -C $dir commit -q -m $message
}

function Get-TreeId([string] $repo) {
  $result = Invoke-Script 'tree-id.ps1' @('-Repo', $repo)
  return ($result.Output -join '').Trim()
}

function Get-RepoState([string] $repo) {
  $index = (git -C $repo rev-parse --path-format=absolute --git-path index)
  return @{
    Cached = (git -C $repo diff --cached --name-status) -join '|'
    Status = (git -C $repo status --porcelain) -join '|'
    Head = git -C $repo rev-parse HEAD
    IndexHash = (Get-FileHash -LiteralPath $index).Hash
    Refs = (git -C $repo for-each-ref) -join '|'
  }
}

# --- tree-id ---------------------------------------------------------------------------------------------------
Write-Host 'tree-id'
$repo = New-Repo 'tree'
Set-Content (Join-Path $repo '.gitignore') '*.log'
New-Item -ItemType Directory -Force (Join-Path $repo 'src') | Out-Null
Set-Content (Join-Path $repo 'src/a.cs') 'class A {}'
Set-Content (Join-Path $repo 'src/b.cs') 'class B {}'
Save-Commit $repo 'base'

$first = Get-TreeId $repo
$second = Get-TreeId $repo
Assert-That ($first -match '^[0-9a-f]{40}$') 'the output is one tree ID'
Assert-That ($first -match '^[0-9a-f]{40}$' -and $first -eq $second) 'the same ID twice without a change'

Add-Content (Join-Path $repo 'src/a.cs') '// edit'
$afterEdit = Get-TreeId $repo
Assert-That ($afterEdit -match '^[0-9a-f]{40}$' -and $afterEdit -ne $first) 'a new ID after an edit of a tracked file'

Set-Content (Join-Path $repo 'src/c.cs') 'class C {}'
$afterNew = Get-TreeId $repo
Assert-That ($afterNew -match '^[0-9a-f]{40}$' -and $afterNew -ne $afterEdit) 'a new ID after a new untracked file'

Remove-Item (Join-Path $repo 'src/b.cs')
$afterDelete = Get-TreeId $repo
Assert-That ($afterDelete -match '^[0-9a-f]{40}$' -and $afterDelete -ne $afterNew) 'a new ID after a deletion'

Set-Content (Join-Path $repo 'noise.log') 'ignored'
Assert-That ($afterDelete -match '^[0-9a-f]{40}$' -and (Get-TreeId $repo) -eq $afterDelete) 'an ignored file changes nothing'

$before = Get-RepoState $repo
[void](Get-TreeId $repo)
$after = Get-RepoState $repo
$isStateSame = $true
foreach ($key in $before.Keys) { if ($before[$key] -ne $after[$key]) { $isStateSame = $false } }
Assert-That ($isStateSame) 'the index, the status, HEAD, and the refs are the same before and after'
Assert-That ($before.Cached -eq '') 'nothing is staged after the run'

Save-Commit $repo 'second'
$worktree = Join-Path $work 'tree-worktree'
git -C $repo worktree add -q -b other $worktree
$worktreeFirst = Get-TreeId $worktree
Set-Content (Join-Path $worktree 'src/w.cs') 'class W {}'
$worktreeAfter = Get-TreeId $worktree
Assert-That ($worktreeFirst -match '^[0-9a-f]{40}$' -and $worktreeAfter -match '^[0-9a-f]{40}$' -and
  $worktreeFirst -ne $worktreeAfter) 'it works in a git worktree'
Assert-That ($worktreeFirst -match '^[0-9a-f]{40}$' -and (Get-TreeId $repo) -eq $worktreeFirst) 'the worktree and the main checkout give the same ID for the same content'

# --- check-plan ------------------------------------------------------------------------------------------------
Write-Host 'check-plan'

function New-PlanFolder([string] $name, [hashtable] $change = @{}) {
  $folder = Join-Path $work $name
  New-Item -ItemType Directory -Force $folder | Out-Null
  $specLines = @(
    '# Spec', '', '## Feature A', '', '*   **AC1:** one', '*   **AC2:** two', '',
    '## Feature B', '', '*   **AC3:** three')
  if ($change.DropAc3Line) { $specLines = $specLines | Where-Object { $_ -notmatch 'AC3' } }
  $specLine1 = if ($change.Cycle1Spec) { $change.Cycle1Spec } else { 'Spec: **Feature A**, AC1-AC2.' }
  $specLine2 = if ($change.Cycle2Spec) { $change.Cycle2Spec } else { 'Spec: **Feature B**, AC3.' }
  $step2 = if ($change.Step2Text) { $change.Step2Text } else { 'Step 2: Do the second thing with the value 5.' }
  $planLines = @('Cycle: 2', '', '# Plan', '', '### Cycle 1', '')
  $planLines += $specLine1, '', '- [x] Step 1: Do the first thing.', '    *   Files: `src/a.cs`', ''
  $planLines += '### Cycle 2', ''
  if (-not $change.NoCycle2Spec) { $planLines += $specLine2, '' }
  $planLines += "- [ ] $step2", '    *   Files: `src/b.cs`', ''
  $planLines += '## Acceptance Criteria Map', '', '| AC | Steps |', '| --- | --- |', '| AC1, AC2 | 1 |'
  if (-not $change.DropAc3Row) { $planLines += '| AC3 | 2 |' }
  $planLines += '', '## Deviations', ''
  Set-Content (Join-Path $folder 'spec.md') $specLines
  Set-Content (Join-Path $folder 'plan.md') $planLines
  return $folder
}

$clean = New-PlanFolder 'plan-clean'
$cleanDefault = Invoke-Script 'check-plan.ps1' @($clean)
$cleanOne = Invoke-Script 'check-plan.ps1' @($clean, '-Cycle', '1')
Assert-That ($cleanDefault.Exit -eq 0 -and $cleanDefault.Output.Count -eq 0) 'a sample plan passes (highest cycle by default)'
Assert-That ($cleanOne.Exit -eq 0 -and $cleanOne.Output.Count -eq 0) 'a sample plan passes (cycle 1)'

$noSpecLine = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-nospec' @{ NoCycle2Spec = $true }))
Assert-That ($noSpecLine.Exit -eq 1 -and $noSpecLine.Output.Count -eq 1 -and $noSpecLine.Output[0] -match 'no Spec: line') `
  'a cycle without a Spec line fails with its line'

$badName = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-badname' @{ Cycle2Spec = 'Spec: **Feature C**, AC3.' }))
Assert-That ($badName.Exit -eq 1 -and $badName.Output.Count -eq 1 -and $badName.Output[0] -match "'Feature C'") `
  'a section name that is no heading of spec.md fails with its line'

$noAcLine = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-noacline' @{ DropAc3Line = $true; DropAc3Row = $false }))
Assert-That ($noAcLine.Exit -eq 1 -and $noAcLine.Output.Count -eq 1 -and $noAcLine.Output[0] -match 'AC3 has no .* line in spec.md') `
  'an AC without a line in spec.md fails with its line'

$noAcRow = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-noacrow' @{ DropAc3Row = $true }))
Assert-That ($noAcRow.Exit -eq 1 -and $noAcRow.Output.Count -eq 1 -and $noAcRow.Output[0] -match 'AC3 has no row') `
  'an AC without a map row fails with its line'

$specRef = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-specref' @{ Step2Text = 'Step 2: Do it as in spec.md.' }))
Assert-That ($specRef.Exit -eq 1 -and $specRef.Output.Count -eq 1 -and $specRef.Output[0] -match 'Step 2 names the spec') `
  'a step that names spec.md fails with its line'

$cycleOneBroken = New-PlanFolder 'plan-cycle1' @{ Cycle1Spec = 'Spec: **Nope**, AC1-AC2.' }
$brokenOne = Invoke-Script 'check-plan.ps1' @($cycleOneBroken, '-Cycle', '1')
$brokenTwo = Invoke-Script 'check-plan.ps1' @($cycleOneBroken, '-Cycle', '2')
$brokenDefault = Invoke-Script 'check-plan.ps1' @($cycleOneBroken)
Assert-That ($brokenOne.Exit -eq 1) 'a problem in cycle 1 fails the check of cycle 1'
Assert-That ($brokenTwo.Exit -eq 0 -and $brokenTwo.Output.Count -eq 0 -and $brokenDefault.Exit -eq 0) `
  'a problem in cycle 1 does not fail the check of cycle 2'

# --- check-handoff ---------------------------------------------------------------------------------------------
Write-Host 'check-handoff'

function New-HandoffCase([string] $name, [hashtable] $change = @{}) {
  $repoDir = New-Repo "$name-repo"
  New-Item -ItemType Directory -Force (Join-Path $repoDir 'src') | Out-Null
  Set-Content (Join-Path $repoDir 'src/a.cs') 'class A {}'
  Save-Commit $repoDir 'base'
  git -C $repoDir tag base
  Add-Content (Join-Path $repoDir 'src/a.cs') '// edit'
  Set-Content (Join-Path $repoDir 'src/b.cs') 'class B {}'
  if ($change.ExtraFile) { Set-Content (Join-Path $repoDir $change.ExtraFile) 'class X {}' }
  $tree = Get-TreeId $repoDir
  if ($change.StaleTree) { Add-Content (Join-Path $repoDir 'src/b.cs') '// late fix' }

  $folder = Join-Path $work "$name-folder"
  New-Item -ItemType Directory -Force $folder | Out-Null
  $step2Mark = if ($change.Step2Open) { ' ' } else { 'x' }
  $lines = @('Cycle: 1', '', '# Plan', '', '### Cycle 1', '', 'Spec: **Feature A**, AC1.', '')
  $lines += '- [x] Step 1: Change the code with a TDD red step.', '    *   Files: `src/a.cs`, `src/b.cs`'
  if (-not $change.NoRedLine) { $lines += '    *   Red: the new test fails first with `Expected 1, found 0`.' }
  $step2Text = if ($change.Step2Tdd) { 'Document it with a TDD red step.' } else { 'Document it.' }
  $lines += "- [$step2Mark] Step 2: $step2Text", '    *   Files: `src/a.cs`'
  if ($change.Step2Red) { $lines += '    *   Red: the doc test fails first with `Expected 2, found 0`.' }
  $lines += ''
  if (-not $change.NoFullRun) { $lines += "*   Full run: tree $tree; dotnet test -> 12 passed, 0 failed" }
  $lines += '', '## Deviations', ''
  if ($change.DeviationFile) { $lines += "*   Steps 1-2: Added $($change.DeviationFile) because the helper was missing." }
  Set-Content (Join-Path $folder 'plan.md') $lines
  return @{ Folder = $folder; Repo = $repoDir }
}

function Invoke-Handoff($case) {
  return Invoke-Script 'check-handoff.ps1' @($case.Folder, '-Repo', $case.Repo, '-Base', 'base')
}

$cleanCase = Invoke-Handoff (New-HandoffCase 'ho-clean')
Assert-That ($cleanCase.Exit -eq 0 -and $cleanCase.Output.Count -eq 0) 'a clean case passes'

$openStep = Invoke-Handoff (New-HandoffCase 'ho-open' @{ Step2Open = $true })
Assert-That ($openStep.Exit -eq 1 -and $openStep.Output.Count -eq 1 -and $openStep.Output[0] -match 'Step 2 is not done') `
  'a step that is not done fails with its line'

$noRed = Invoke-Handoff (New-HandoffCase 'ho-nored' @{ NoRedLine = $true })
Assert-That ($noRed.Exit -eq 1 -and $noRed.Output.Count -eq 1 -and $noRed.Output[0] -match 'Step 1 names TDD or Red') `
  'a TDD step without a Red: line fails with its line'

$secondNoRed = Invoke-Handoff (New-HandoffCase 'ho-second' @{ Step2Tdd = $true })
Assert-That ($secondNoRed.Exit -eq 1 -and $secondNoRed.Output.Count -eq 1 -and
  $secondNoRed.Output[0] -match 'Step 2 names TDD or Red') `
  'a second TDD step without a Red: line fails, although the first step has one'

$bothRed = Invoke-Handoff (New-HandoffCase 'ho-both' @{ Step2Tdd = $true; Step2Red = $true })
Assert-That ($bothRed.Exit -eq 0 -and $bothRed.Output.Count -eq 0) 'two TDD steps that each have a Red: line pass'

$extra = Invoke-Handoff (New-HandoffCase 'ho-extra' @{ ExtraFile = 'src/extra.cs' })
Assert-That ($extra.Exit -eq 1 -and $extra.Output.Count -eq 1 -and $extra.Output[0] -match 'src/extra.cs is in no Files') `
  'a changed file that no Files: line names fails with its line'

$logged = Invoke-Handoff (New-HandoffCase 'ho-logged' @{ ExtraFile = 'src/extra.cs'; DeviationFile = 'src/extra.cs' })
Assert-That ($logged.Exit -eq 0 -and $logged.Output.Count -eq 0) 'a changed file in a Deviations entry passes'

$noRun = Invoke-Handoff (New-HandoffCase 'ho-norun' @{ NoFullRun = $true })
Assert-That ($noRun.Exit -eq 1 -and $noRun.Output.Count -eq 1 -and $noRun.Output[0] -match "no 'Full run") `
  'a missing Full run: line fails with its line'

$stale = Invoke-Handoff (New-HandoffCase 'ho-stale' @{ StaleTree = $true })
Assert-That ($stale.Exit -eq 1 -and $stale.Output.Count -eq 1 -and $stale.Output[0] -match 'is not the current tree') `
  'a Full run: tree that is not the current tree fails with its line'

# --- check-review-routing --------------------------------------------------------------------------------------
Write-Host 'check-review-routing'

function New-ReviewFile([string] $name, [bool] $hasCheckpoint) {
  $path = Join-Path $work "$name.md"
  $lines = @(
    'Cycle: 1', 'Round: 1', '', '# Review', '', '## Open Findings', '',
    '| Severity | [CODE] | [SPEC] |', '|---|---|---|', '| BLOCKER | 0 | 0 |', '| MAJOR | 1 | 0 |', '| MINOR | 0 | 0 |', '',
    'Verdict: CHANGES_REQUESTED', '')
  if ($hasCheckpoint) {
    $lines += '## Checkpoint 1', '', '### F1 MAJOR [SPEC] A checkpoint finding', '',
    '| Severity | [CODE] | [SPEC] |', '|---|---|---|', '| BLOCKER | 0 | 2 |', '| MAJOR | 0 | 0 |', '| MINOR | 0 | 0 |', ''
  }
  Set-Content $path $lines
  return $path
}

$withoutCheckpoint = Invoke-Script 'check-review-routing.ps1' @((New-ReviewFile 'review-plain' $false))
$withCheckpoint = Invoke-Script 'check-review-routing.ps1' @((New-ReviewFile 'review-checkpoint' $true))
Assert-That ($withoutCheckpoint.Output -join '' -match '^executor code-fixes') 'a review without a checkpoint routes to the executor'
Assert-That ($withCheckpoint.Exit -eq 0 -and ($withCheckpoint.Output -join '') -match '^executor code-fixes') `
  'a checkpoint section with [SPEC] findings after the final verdict does not change the route'

Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
if ($failures.Count -gt 0) {
  Write-Host "`n$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host "`nAll checks passed."
