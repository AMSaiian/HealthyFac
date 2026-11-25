#Requires -Version 7
# Deterministic check of the flow scripts: tree-id.ps1, check-plan.ps1, check-handoff.ps1, check-review-routing.ps1,
# commit-cycle.ps1, format-changed.ps1, and run-area.ps1. It builds throw-away git repositories and feature folders in
# a temp folder and runs the real scripts, with fake dotnet, npm, and npx commands for the last two. It uses no test
# framework (same form as .claude/hooks/tests/hook-scope.tests.ps1).
#
# Cases:
# - tree-id: the same ID twice without a change; a new ID after an edit of a tracked file, a new untracked file, and a
#   deletion; an ignored file changes nothing; the index, the status, and HEAD are the same before and after; it works
#   in a git worktree.
# - tree-id -Area: see the area cases of check-handoff (a backend-only change needs no frontend run); -Area with -Ref
#   of the base works when the branch has a commit after the base.
# - check-plan: a sample plan with cycles 1 and 2 passes; each problem (Spec line, section name, AC line, map row,
#   spec reference, test step without a proof marker, no "#### Deviations", duplicate AC, a full run in a Verify line,
#   a template or style step without a Look: line, a "Browser check: none" reason quoted in two cycles) fails alone
#   with its line; a problem in cycle 1 does not fail a check of cycle 2.
# - check-handoff: a clean case passes; each problem fails alone with its line; each TDD step is checked on its own;
#   the area full runs (backend only, stale, frontend missing, frontend of an earlier cycle); the cycle Deviations;
#   a Proof: line or a "done" log in the Deviations; a long C# line (Migrations/ skipped); a formatter change;
#   the default base (merge base with the main branch); a failing script test of a changed flow script; an old
#   "Full run: tree" line and an old plan-wide "## Deviations" count for nothing; the browser flow lines (a pass line
#   without seen:, a pass line with a caveat, a transient fail without retried: fail; a retry note and step prose pass).
# - check-review-routing: the "## Open" layout routes, counts REOPENED only in ## Open, and fails on a wrong table, a
#   wrong heading, APPROVE with an open MAJOR, or a review.md without "## Open".
# - format-changed: a second call at the same tree formats nothing again; a new file at the same tree is formatted
#   alone; a changed tree formats every given file again.
# - run-area: the backend order (format, build, unit, integration) and the Full run line with the tree after the
#   format step; a failing unit command, a long line, and a change during the run fail it; the frontend commands with
#   and without a keycloak-theme change.
# - commit-cycle: an approved cycle is committed as one commit for each group (tooling, docs, code) with the subject
#   only, nothing is pushed, a rerun makes no commit; each refusal (no APPROVE, another cycle, a changed tree, a changed
#   file without a Commit line, a Commit line without a changed file, a bad subject) changes nothing.
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

$areaRepo = New-Repo 'tree-area-ref'
New-Item -ItemType Directory -Force (Join-Path $areaRepo 'src'), (Join-Path $areaRepo 'frontend') | Out-Null
Set-Content (Join-Path $areaRepo 'src/a.cs') 'class A {}'
Set-Content (Join-Path $areaRepo 'frontend/app.ts') 'export const a = 1;'
Save-Commit $areaRepo 'base'
$areaBase = git -C $areaRepo rev-parse HEAD
Add-Content (Join-Path $areaRepo 'src/a.cs') '// committed'
Add-Content (Join-Path $areaRepo 'frontend/app.ts') '// committed'
Save-Commit $areaRepo 'feature'
foreach ($area in @('backend', 'frontend')) {
  $atBase = Invoke-Script 'tree-id.ps1' @('-Repo', $areaRepo, '-Area', $area, '-Ref', $areaBase)
  Assert-That ($atBase.Exit -eq 0 -and ($atBase.Output -join '').Trim() -match '^[0-9a-f]{40}$') `
    "-Area $area -Ref <base> works when the branch has a commit after the base"
}

# --- check-plan ------------------------------------------------------------------------------------------------
Write-Host 'check-plan'

function New-PlanFolder([string] $name, [hashtable] $change = @{}) {
  $folder = Join-Path $work $name
  New-Item -ItemType Directory -Force $folder | Out-Null
  $specLines = @(
    '# Spec', '', '## Feature A', '', '*   **AC1:** one', '*   **AC2:** two', '',
    '## Feature B', '', '*   **AC3:** three')
  if ($change.DropAc3Line) { $specLines = $specLines | Where-Object { $_ -notmatch 'AC3' } }
  if ($change.DuplicateAc2) { $specLines += '*   **AC2:** two again' }
  $specLine1 = if ($change.Cycle1Spec) { $change.Cycle1Spec } else { 'Spec: **Feature A**, AC1-AC2.' }
  $specLine2 = if ($change.Cycle2Spec) { $change.Cycle2Spec } else { 'Spec: **Feature B**, AC3.' }
  $step2 = if ($change.Step2Text) { $change.Step2Text } else { 'Step 2: Do the second thing with the value 5.' }
  $step2Files = if ($change.Step2Files) { $change.Step2Files } else { '`src/b.cs`' }
  $cycle1Commits = if ($null -ne $change.Cycle1Commits) { $change.Cycle1Commits } else { @('Commit code: feature(sample): added a') }
  $cycle2Commits = if ($null -ne $change.Cycle2Commits) { $change.Cycle2Commits } else { @('Commit code: feature(sample): added b') }
  $firstLine = if ($change.FirstLine) { $change.FirstLine } else { 'Cycle: 2' }
  $planLines = @($firstLine, '', '# Plan', '', '### Cycle 1', '')
  $planLines += $specLine1
  $planLines += $cycle1Commits
  if ($change.Cycle1Lines) { $planLines += $change.Cycle1Lines }
  $planLines += '', '- [x] Step 1: Do the first thing.', '    *   Files: `src/a.cs`', '', '#### Deviations', ''
  $planLines += '### Cycle 2', ''
  if (-not $change.NoCycle2Spec) { $planLines += $specLine2 }
  $planLines += $cycle2Commits
  if ($change.Cycle2Lines) { $planLines += $change.Cycle2Lines }
  $planLines += ''
  $planLines += "- [ ] $step2", "    *   Files: $step2Files"
  if ($change.Step2Look) { $planLines += "    *   Look: $($change.Step2Look)" }
  if ($change.Step2Verify) { $planLines += "    *   Verify: $($change.Step2Verify)" }
  $planLines += ''
  if (-not $change.NoCycle2Deviations) { $planLines += '#### Deviations', '' }
  $planLines += '## Acceptance Criteria Map', '', '| AC | Steps |', '| --- | --- |', '| AC1, AC2 | 1 |'
  if (-not $change.DropAc3Row) { $planLines += '| AC3 | 2 |' }
  Set-Content (Join-Path $folder 'spec.md') $specLines
  Set-Content (Join-Path $folder 'plan.md') $planLines
  return $folder
}

$clean = New-PlanFolder 'plan-clean'
$cleanDefault = Invoke-Script 'check-plan.ps1' @($clean)
$cleanOne = Invoke-Script 'check-plan.ps1' @($clean, '-Cycle', '1')
Assert-That ($cleanDefault.Exit -eq 0 -and $cleanDefault.Output.Count -eq 0) 'a sample plan passes (the Cycle: line by default)'
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

$specCommand = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-speccommand' @{ Step2Files = '`.claude/commands/spec.md`' }))
Assert-That ($specCommand.Exit -eq 0 -and $specCommand.Output.Count -eq 0) `
  'a step that names the path .claude/commands/spec.md passes'

foreach ($case in @(
    @('no commit line', @{ Cycle2Commits = @() }, 'Cycle 2: no Commit line.'),
    @('an unknown commit group', @{ Cycle2Commits = @('Commit tests: feature(sample): added b') }, "unknown Commit group 'tests'"),
    @('two lines of one group', @{ Cycle2Commits = @('Commit code: feature(sample): added b', 'Commit code: feature(sample): added c') },
      "2 Commit lines for the group 'code'"),
    @('a subject in upper case', @{ Cycle2Commits = @('Commit code: Feature(sample): Added b') }, "is not '<feature|bug>"),
    @('a tooling subject without setup', @{ Cycle2Commits = @('Commit tooling: feature(sample): added b') }, 'needs the category setup'))) {
  $commitCase = Invoke-Script 'check-plan.ps1' @((New-PlanFolder "plan-commit-$($case[0].Length)" $case[1]))
  Assert-That ($commitCase.Exit -eq 1 -and $commitCase.Output.Count -eq 1 -and $commitCase.Output[0] -like "*$($case[2])*") `
    "a cycle with $($case[0]) fails with its line"
}

$openCycleOne = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-open1-ok' @{ FirstLine = 'Cycle: 1'; Cycle2Commits = @() }))
Assert-That ($openCycleOne.Exit -eq 0 -and $openCycleOne.Output.Count -eq 0) `
  'with Cycle: 1 as the first line, a problem only in cycle 2 does not fail the default check'
$openCycleOneBroken = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-open1-bad' @{ FirstLine = 'Cycle: 1'; Cycle1Commits = @() }))
Assert-That ($openCycleOneBroken.Exit -eq 1 -and $openCycleOneBroken.Output[0] -like 'Cycle 1: no Commit line.*') `
  'with Cycle: 1 as the first line, a problem in cycle 1 fails the default check'

$cycleOneBroken = New-PlanFolder 'plan-cycle1' @{ Cycle1Spec = 'Spec: **Nope**, AC1-AC2.' }
$brokenOne = Invoke-Script 'check-plan.ps1' @($cycleOneBroken, '-Cycle', '1')
$brokenTwo = Invoke-Script 'check-plan.ps1' @($cycleOneBroken, '-Cycle', '2')
$brokenDefault = Invoke-Script 'check-plan.ps1' @($cycleOneBroken)
Assert-That ($brokenOne.Exit -eq 1) 'a problem in cycle 1 fails the check of cycle 1'
Assert-That ($brokenTwo.Exit -eq 0 -and $brokenTwo.Output.Count -eq 0 -and $brokenDefault.Exit -eq 0) `
  'a problem in cycle 1 does not fail the check of cycle 2'

$testStep = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-teststep' @{ Step2Files = '`tests/BTests.cs`' }))
Assert-That ($testStep.Exit -eq 1 -and $testStep.Output.Count -eq 1 -and $testStep.Output[0] -match 'Step 2 changes a test file') `
  'a step that changes a test file without a proof marker fails with its line'

foreach ($marker in 'TDD', 'Proof required', 'No proof: a rename only') {
  $marked = Invoke-Script 'check-plan.ps1' @((New-PlanFolder "plan-marker-$($marker.Length)" @{
        Step2Files = '`frontend/src/b.spec.ts`'; Step2Text = "Step 2: Test the second thing. $marker" }))
  Assert-That ($marked.Exit -eq 0 -and $marked.Output.Count -eq 0) "a test step with the marker '$marker' passes"
}

$noDeviations = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-nodev' @{ NoCycle2Deviations = $true }))
Assert-That ($noDeviations.Exit -eq 1 -and $noDeviations.Output.Count -eq 1 -and
  $noDeviations.Output[0] -match "no '#### Deviations' heading") 'a cycle without its Deviations heading fails with its line'

foreach ($case in @(
    @('verify-run', 'then the full backend run again', $false),
    @('verify-commands', 'the filtered tests, then the full commands of the area', $false),
    @('verify-filter', '`dotnet test --project tests/B --filter "FullyQualifiedName~BTests"`', $true))) {
  $verifyCase = Invoke-Script 'check-plan.ps1' @((New-PlanFolder "plan-$($case[0])" @{ Step2Verify = $case[1] }))
  if ($case[2]) {
    Assert-That ($verifyCase.Exit -eq 0 -and $verifyCase.Output.Count -eq 0) "a Verify line '$($case[1])' passes"
  } else {
    Assert-That ($verifyCase.Exit -eq 1 -and $verifyCase.Output.Count -eq 1 -and
      $verifyCase.Output[0] -eq 'Cycle 2: Step 2 asks for a full run in its Verify line. Name test filters; the executor runs the full run once.') `
      "a Verify line '$($case[1])' fails with its line"
  }
}

$verifyProse = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-verify-prose' @{
      Step2Text = 'Step 2: Say that the Verify line never asks for a full run.'; Step2Verify = '`dotnet test --filter BTests`' }))
Assert-That ($verifyProse.Exit -eq 0 -and $verifyProse.Output.Count -eq 0) `
  'a step text that names the Verify line and a full run passes; only the Verify: bullet counts'

$duplicateAc = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-dupac' @{ DuplicateAc2 = $true }))
Assert-That ($duplicateAc.Exit -eq 1 -and $duplicateAc.Output.Count -eq 1 -and $duplicateAc.Output[0] -match '2 lines for AC2') `
  'an AC number with two lines in spec.md fails with its line'

foreach ($uiFiles in @('`frontend/src/app/b/b.html`', '`frontend/src/app/b/b.css`', '`frontend/src/app/b/` (template, utils)')) {
  $noLook = Invoke-Script 'check-plan.ps1' @((New-PlanFolder "plan-nolook-$($uiFiles.Length)" @{ Step2Files = $uiFiles }))
  Assert-That ($noLook.Exit -eq 1 -and $noLook.Output.Count -eq 1 -and $noLook.Output[0] -match 'Step 2 changes a template or a style file, but has no Look: line') `
    "a step with $uiFiles in its Files: line and no Look: line fails with its line"
}
$withLook = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-look' @{
      Step2Files = '`frontend/src/app/b/b.html`'; Step2Look = 'as develop `frontend/src/app/b/b.html`' }))
Assert-That ($withLook.Exit -eq 0 -and $withLook.Output.Count -eq 0) 'a template step with a Look: line passes'

$noCheck = 'Browser check: none (Artur, 2026-11-23: "you can perform no UI check, I will do just ask").'
$repeatedNone = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-none-repeat' @{ Cycle1Lines = @($noCheck); Cycle2Lines = @($noCheck) }))
Assert-That ($repeatedNone.Exit -eq 1 -and $repeatedNone.Output.Count -eq 1 -and
  $repeatedNone.Output[0] -match "Cycle 2: 'Browser check: none' repeats the reason of cycle 1") `
  'a Browser check: none line that quotes the reason of an earlier cycle fails with its line'
$singleNone = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-none-single' @{ Cycle2Lines = @($noCheck) }))
$otherNone = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-none-other' @{
      Cycle1Lines = @($noCheck); Cycle2Lines = @('Browser check: none (Artur, 2026-11-24: "skip the browser for this lint cycle").') }))
$unquotedNone = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-none-unquoted' @{
      Cycle1Lines = @('Browser check: none (spec files and lint only).'); Cycle2Lines = @('Browser check: none (spec files and lint only).') }))
Assert-That ($singleNone.Exit -eq 0 -and $otherNone.Exit -eq 0 -and $unquotedNone.Exit -eq 0) `
  'one Browser check: none reason, another reason per cycle, and an unquoted reason pass'

# --- plan-sections ---------------------------------------------------------------------------------------------
Write-Host 'plan-sections'
. (Join-Path $ScriptsDir 'plan-sections.ps1')

foreach ($case in @(
    @('.claude/check-plan.ps1', 'tooling'), @('scripts/verify-stack.ps1', 'tooling'), @('CLAUDE.md', 'tooling'),
    @('docs/architecture.md', 'docs'), @('README.md', 'docs'), @('backend/src/a.cs', 'code'),
    @('frontend/src/app.ts', 'code'))) {
  Assert-That ((Get-CommitGroup $case[0]) -eq $case[1]) "the commit group of $($case[0]) is $($case[1])"
}

$cycleLines = @('Cycle: 1', '', '### Cycle 1', 'Spec: a', '', '### Cycle 2', 'Spec: b')
Assert-That ((Get-PlanCycle $cycleLines 0).Number -eq 1) 'cycle 0 reads the Cycle: line of plan.md'
Assert-That ((Get-PlanCycle ($cycleLines | Select-Object -Skip 1) 0).Number -eq 2) 'cycle 0 without a Cycle: line takes the highest cycle'

$commitSection = @('Spec: a', 'Commit code: bug(machines): fixed a', 'Commit docs: feature(docs): described a', '', '- [ ] Step 1: x',
  '#### Deviations', 'Commit tooling: feature(setup): not a commit line')
$commits = @(Get-CycleCommits $commitSection)
Assert-That ($commits.Count -eq 2 -and $commits[0].Group -eq 'code' -and $commits[0].Subject -eq 'bug(machines): fixed a' -and
  $commits[1].Group -eq 'docs') 'the commit lines of a cycle are read before its first #### heading'

function New-Commit([string] $group, [string] $subject) { [pscustomobject]@{ Group = $group; Subject = $subject } }
$goodCommits = @((New-Commit 'tooling' 'feature(setup): added a'), (New-Commit 'docs' 'feature(docs): described a'),
  (New-Commit 'code' 'bug(machines): fixed a'))
Assert-That (@(Get-CommitProblems $goodCommits).Count -eq 0) 'three valid commit lines give no commit problem'
foreach ($case in @(
    @('no commit line', @(), 'no Commit line.'),
    @('an unknown group', @((New-Commit 'tests' 'feature(sample): added b')), "unknown Commit group 'tests'"),
    @('two lines of one group', @((New-Commit 'code' 'feature(sample): a'), (New-Commit 'code' 'feature(sample): b')),
      "2 Commit lines for the group 'code'"),
    @('a subject in upper case', @((New-Commit 'code' 'Feature(sample): Added b')), 'is not ''<feature|bug>'),
    @('a tooling subject without setup', @((New-Commit 'tooling' 'feature(sample): added b')), 'needs the category setup'),
    @('a docs subject without docs', @((New-Commit 'docs' 'feature(sample): added b')), 'needs the category docs'))) {
  $problems = @(Get-CommitProblems $case[1])
  Assert-That ($problems.Count -eq 1 -and $problems[0] -like "*$($case[2])*") "Get-CommitProblems reports $($case[0])"
}

# --- check-handoff ---------------------------------------------------------------------------------------------
Write-Host 'check-handoff'

function Get-AreaTreeId([string] $repo, [string] $area) {
  $result = Invoke-Script 'tree-id.ps1' @('-Repo', $repo, '-Area', $area)
  return ($result.Output -join '').Trim()
}

# A plan with a "Full run backend:" line and "#### Deviations" in the cycle.
function New-HandoffCase([string] $name, [hashtable] $change = @{}) {
  $repoDir = New-Repo "$name-repo"
  New-Item -ItemType Directory -Force (Join-Path $repoDir 'src') | Out-Null
  Set-Content (Join-Path $repoDir 'src/a.cs') 'class A {}'
  if ($change.LongLine) { Set-Content (Join-Path $repoDir '.editorconfig') @('root = true', '[*]', 'max_line_length = 40') }
  Save-Commit $repoDir 'base'
  git -C $repoDir tag base
  Add-Content (Join-Path $repoDir 'src/a.cs') '// edit'
  Set-Content (Join-Path $repoDir 'src/b.cs') 'class B {}'
  if ($change.LongLine) {
    Add-Content (Join-Path $repoDir 'src/b.cs') ('// ' + ('x' * 50))
    New-Item -ItemType Directory -Force (Join-Path $repoDir 'src/Migrations') | Out-Null
    Set-Content (Join-Path $repoDir 'src/Migrations/m.cs') ('// ' + ('m' * 50))
  }
  if ($change.FrontendFile) {
    New-Item -ItemType Directory -Force (Join-Path $repoDir 'frontend') | Out-Null
    Set-Content (Join-Path $repoDir 'frontend/app.ts') 'export const a = 1;'
  }
  if ($change.ExtraFile) { Set-Content (Join-Path $repoDir $change.ExtraFile) 'class X {}' }
  if ($change.ScriptChange) {
    New-Item -ItemType Directory -Force (Join-Path $repoDir '.claude/tests') | Out-Null
    Set-Content (Join-Path $repoDir '.claude/tree-id.ps1') '# changed'
    Set-Content (Join-Path $repoDir '.claude/tests/flow-scripts.tests.ps1') @('Write-Host "  FAIL the sample check"', 'exit 1')
  }
  $backendTree = Get-AreaTreeId $repoDir 'backend'
  $frontendTree = Get-AreaTreeId $repoDir 'frontend'
  if ($change.StaleTree) { Add-Content (Join-Path $repoDir 'src/b.cs') '// late fix' }
  if ($change.LateClaudeFile) {
    New-Item -ItemType Directory -Force (Join-Path $repoDir '.claude/commands') | Out-Null
    Set-Content (Join-Path $repoDir '.claude/commands/late.md') '# a late command change'
  }

  $folder = Join-Path $work "$name-folder"
  New-Item -ItemType Directory -Force $folder | Out-Null
  $step2Mark = if ($change.Step2Open) { ' ' } else { 'x' }
  $lines = @('Cycle: 1', '', '# Plan', '')
  if ($change.EarlierFrontendRun -or $change.EarlierFilesLine -or $change.EarlierDeviationFile) {
    $lines += '### Cycle 0', ''
    if ($change.EarlierFilesLine) { $lines += '- [x] Step 0: Add the extra file.', "    *   Files: ``$($change.EarlierFilesLine)``" }
    if ($change.EarlierFrontendRun) { $lines += "*   Full run frontend: tree $frontendTree; ng test -> 5 passed, 0 failed" }
    if ($change.EarlierDeviationFile) { $lines += '', '#### Deviations', "*   Step 0: Added $($change.EarlierDeviationFile) because the helper was missing." }
    $lines += ''
  }
  $lines += '### Cycle 1', '', 'Spec: **Feature A**, AC1.', ''
  $lines += '- [x] Step 1: Change the code with a TDD red step.', '    *   Files: `src/a.cs`, `src/b.cs`, `src/Migrations/m.cs`, `frontend/app.ts`, `.claude/tree-id.ps1`, `.claude/tests/flow-scripts.tests.ps1`'
  if (-not $change.NoRedLine) { $lines += '    *   Red: the new test fails first with `Expected 1, found 0`.' }
  $step2Text = if ($change.Step2Tdd) { 'Document it with a TDD red step.' } elseif ($change.Step2ProofRequired) { 'Test it. Proof required.' } else { 'Document it.' }
  $lines += "- [$step2Mark] Step 2: $step2Text", '    *   Files: `src/a.cs`'
  if ($change.Step2Red) { $lines += '    *   Red: the doc test fails first with `Expected 2, found 0`.' }
  if ($change.Step2Lines) { $lines += $change.Step2Lines }
  $lines += ''
  if (-not $change.NoFullRun) { $lines += "*   Full run backend: tree $backendTree; dotnet test -> 12 passed, 0 failed" }
  $lines += '', '#### Deviations', ''
  if ($change.DeviationFile) { $lines += "*   Steps 1-2: Added $($change.DeviationFile) because the helper was missing." }
  if ($change.DeviationProof) { $lines += '*   Step 1: Proof: ATest -> Expected 1, found 0; restored, checked with git diff' }
  if ($change.DeviationDone) { $lines += '*   Step 2: done. Files and tests as planned.' }
  if ($change.PlannedCycle2) {
    $lines += '', '### Cycle 2', '', 'Spec: **Feature A**, AC1.', '', '- [ ] Step 3: Plan the next thing.', '    *   Files: `src/c.cs`'
  }
  $lines += '', '## Acceptance Criteria Map', ''
  Set-Content (Join-Path $folder 'plan.md') $lines
  return @{ Folder = $folder; Repo = $repoDir }
}

function Invoke-Handoff($case, [string[]] $extra = @(), [string[]] $base = @('-Base', 'base')) {
  return Invoke-Script 'check-handoff.ps1' (@($case.Folder, '-Repo', $case.Repo) + $base + $extra)
}

$cleanCase = Invoke-Handoff (New-HandoffCase 'ho-clean')
Assert-That ($cleanCase.Exit -eq 0 -and $cleanCase.Output.Count -eq 0) 'a clean case passes'

$goodFlows = Invoke-Handoff (New-HandoffCase 'ho-flows-ok' @{ Step2Lines = @(
      '    *   pass: the drawer opens over the board; seen: 1280 x 600 light, 1920 x 1080 dark',
      '    *   round 2: pass: the chip follows the cursor; seen: 1280 x 600 light; retried: the screenshot timed out once',
      '    *   Flow assign: pass; the toast shows; seen: 1920 x 1080 dark',
      '    *   fail: the badge sits above the button',
      '    *   Pass the token to the call.') })
Assert-That ($goodFlows.Exit -eq 0 -and $goodFlows.Output.Count -eq 0) `
  'pass lines with seen: (also after a retry), a fail line without a transient error, and step prose pass'
$flowCases = @(
  @('ho-flow-noseen', '    *   pass: the drawer opens over the board', "has no 'seen:"),
  @('ho-flow-script', '    *   pass: drags were sent as mouse events from the page; seen: 1280 x 600 light', 'a pass line has a caveat'),
  @('ho-flow-zoom', '    *   pass: full HD through a root zoom; seen: 1920 x 1080 dark', 'a pass line has a caveat'),
  @('ho-flow-timeout', '    *   pass: labels inside the board, the screenshot timed out; seen: 1280 x 600 light', 'a pass line has a caveat'),
  @('ho-flow-stub', '    *   pass: pointer events on the handle (pointer capture stubbed); seen: 1280 x 600 light', 'a pass line has a caveat'),
  @('ho-flow-transient', '    *   fail: Chrome screenshots timed out', "a transient error without 'retried:'"))
foreach ($case in $flowCases) {
  $flowCase = Invoke-Handoff (New-HandoffCase $case[0] @{ Step2Lines = @($case[1]) })
  Assert-That ($flowCase.Exit -eq 1 -and $flowCase.Output.Count -eq 1 -and $flowCase.Output[0] -like "Step 2: *$($case[2])*") `
    "the flow line '$($case[1].Trim())' fails with its line"
}
$retriedFail = Invoke-Handoff (New-HandoffCase 'ho-flow-retried' @{ Step2Lines = @('    *   fail: the slot did not start; retried: up') })
Assert-That ($retriedFail.Exit -eq 0 -and $retriedFail.Output.Count -eq 0) 'a fail line of a transient error with retried: passes'

$openStep = Invoke-Handoff (New-HandoffCase 'ho-open' @{ Step2Open = $true })
Assert-That ($openStep.Exit -eq 1 -and $openStep.Output.Count -eq 1 -and $openStep.Output[0] -match 'Step 2 is not done') `
  'a step that is not done fails with its line'

$noRed = Invoke-Handoff (New-HandoffCase 'ho-nored' @{ NoRedLine = $true })
Assert-That ($noRed.Exit -eq 1 -and $noRed.Output.Count -eq 1 -and $noRed.Output[0] -match 'Step 1 names TDD, Red, or Proof required') `
  'a TDD step without a Red: line fails with its line'

$secondNoRed = Invoke-Handoff (New-HandoffCase 'ho-second' @{ Step2Tdd = $true })
Assert-That ($secondNoRed.Exit -eq 1 -and $secondNoRed.Output.Count -eq 1 -and
  $secondNoRed.Output[0] -match 'Step 2 names TDD, Red, or Proof required') `
  'a second TDD step without a Red: line fails, although the first step has one'

$bothRed = Invoke-Handoff (New-HandoffCase 'ho-both' @{ Step2Tdd = $true; Step2Red = $true })
Assert-That ($bothRed.Exit -eq 0 -and $bothRed.Output.Count -eq 0) 'two TDD steps that each have a Red: line pass'

$extra = Invoke-Handoff (New-HandoffCase 'ho-extra' @{ ExtraFile = 'src/extra.cs' })
Assert-That ($extra.Exit -eq 1 -and $extra.Output.Count -eq 1 -and $extra.Output[0] -match 'src/extra.cs is in no Files') `
  'a changed file that no Files: line names fails with its line'

$logged = Invoke-Handoff (New-HandoffCase 'ho-logged' @{ ExtraFile = 'src/extra.cs'; DeviationFile = 'src/extra.cs' })
Assert-That ($logged.Exit -eq 0 -and $logged.Output.Count -eq 0) 'a changed file in a Deviations entry passes'

$noRun = Invoke-Handoff (New-HandoffCase 'ho-norun' @{ NoFullRun = $true })
Assert-That ($noRun.Exit -eq 1 -and $noRun.Output.Count -eq 1 -and $noRun.Output[0] -match "no 'Full run backend") `
  'a missing Full run backend line fails with its line'

$oldRun = New-HandoffCase 'ho-old-run' @{ NoFullRun = $true }
$oldPlan = Join-Path $oldRun.Folder 'plan.md'
$oldText = (Get-Content $oldPlan -Raw).Replace('#### Deviations', "*   Full run: tree $(Get-TreeId $oldRun.Repo); dotnet test -> 12 passed, 0 failed`n`n#### Deviations")
Set-Content $oldPlan $oldText
$oldRunResult = Invoke-Handoff $oldRun
Assert-That ($oldRunResult.Exit -eq 1 -and $oldRunResult.Output.Count -eq 1 -and $oldRunResult.Output[0] -match "no 'Full run backend") `
  'an old "Full run: tree" line with the current tree does not count as a full run'

$plannedCycle = Invoke-Handoff (New-HandoffCase 'ho-planned' @{PlannedCycle2 = $true })
Assert-That ($plannedCycle.Exit -eq 0 -and $plannedCycle.Output.Count -eq 0) `
  'a planned cycle 2 below the open cycle 1 does not fail the handoff of cycle 1'

$areaStale = Invoke-Handoff (New-HandoffCase 'ho-area-stale' @{StaleTree = $true })
Assert-That ($areaStale.Exit -eq 1 -and $areaStale.Output.Count -eq 1 -and $areaStale.Output[0] -match 'not the current backend tree') `
  'a Full run backend line with an old tree fails with its line'

$areaFrontend = Invoke-Handoff (New-HandoffCase 'ho-area-fe' @{FrontendFile = $true })
Assert-That ($areaFrontend.Exit -eq 1 -and $areaFrontend.Output.Count -eq 1 -and
  $areaFrontend.Output[0] -match "frontend area changed .* no 'Full run frontend") 'a changed frontend without its Full run line fails'

$areaEarlier = Invoke-Handoff (New-HandoffCase 'ho-area-earlier' @{FrontendFile = $true; EarlierFrontendRun = $true })
Assert-That ($areaEarlier.Exit -eq 0 -and $areaEarlier.Output.Count -eq 0) `
  'a Full run frontend line of an earlier cycle with the current tree passes'

$earlierFiles = Invoke-Handoff (New-HandoffCase 'ho-earlier-files' @{ExtraFile = 'src/extra.cs'; EarlierFilesLine = 'src/extra.cs' })
Assert-That ($earlierFiles.Exit -eq 0 -and $earlierFiles.Output.Count -eq 0) `
  'a changed file in a Files: line of an earlier cycle passes (the default base holds the earlier cycles)'

$earlierDeviation = Invoke-Handoff (New-HandoffCase 'ho-earlier-dev' @{ExtraFile = 'src/extra.cs'; EarlierDeviationFile = 'src/extra.cs' })
Assert-That ($earlierDeviation.Exit -eq 0 -and $earlierDeviation.Output.Count -eq 0) `
  'a changed file in the #### Deviations of an earlier cycle passes'

$lateClaude = Invoke-Handoff (New-HandoffCase 'ho-late-claude' @{LateClaudeFile = $true; DeviationFile = '.claude/commands/late.md' })
Assert-That ($lateClaude.Exit -eq 0 -and $lateClaude.Output.Count -eq 0) `
  'a .claude change after the Full run backend line keeps the backend run current'

$cycleLogged = Invoke-Handoff (New-HandoffCase 'ho-cycle-dev' @{ExtraFile = 'src/extra.cs'; DeviationFile = 'src/extra.cs' })
Assert-That ($cycleLogged.Exit -eq 0 -and $cycleLogged.Output.Count -eq 0) 'a changed file in the #### Deviations of the cycle passes'

$planWide = New-HandoffCase 'ho-plan-wide' @{ ExtraFile = 'src/extra.cs' }
Add-Content (Join-Path $planWide.Folder 'plan.md') @('', '## Deviations', '', '*   Steps 1-2: Added src/extra.cs because the helper was missing.')
$planWideResult = Invoke-Handoff $planWide
Assert-That ($planWideResult.Exit -eq 1 -and $planWideResult.Output.Count -eq 1 -and $planWideResult.Output[0] -match 'src/extra.cs is in no Files') `
  'a changed file that only an old plan-wide ## Deviations entry names fails'

$devProof = Invoke-Handoff (New-HandoffCase 'ho-dev-proof' @{DeviationProof = $true })
Assert-That ($devProof.Exit -eq 1 -and $devProof.Output.Count -eq 1 -and $devProof.Output[0] -match 'holds a Red: or Proof: line') `
  'a Proof: line in the Deviations fails, and it does not count for the step block'

$devDone = Invoke-Handoff (New-HandoffCase 'ho-dev-done' @{DeviationDone = $true })
Assert-That ($devDone.Exit -eq 1 -and $devDone.Output.Count -eq 1 -and $devDone.Output[0] -match 'only logs a run') `
  'a "Step N: done" entry in the Deviations fails with its line'

$proofRequired = Invoke-Handoff (New-HandoffCase 'ho-proof-required' @{Step2ProofRequired = $true })
Assert-That ($proofRequired.Exit -eq 1 -and $proofRequired.Output.Count -eq 1 -and $proofRequired.Output[0] -match 'Step 2 names TDD, Red, or Proof required') `
  'a Proof required step without a Red: or Proof: line fails with its line'

$longLine = Invoke-Handoff (New-HandoffCase 'ho-long' @{LongLine = $true })
Assert-That ($longLine.Exit -eq 1 -and $longLine.Output.Count -eq 1 -and $longLine.Output[0] -match '^src/b.cs:2 has 53 characters') `
  'a long line of a changed C# file fails with its line; a long line in Migrations/ passes'

$defaultBase = Invoke-Handoff (New-HandoffCase 'ho-default-base' @{}) @() @('-MainBranch', 'main')
Assert-That ($defaultBase.Exit -eq 0 -and $defaultBase.Output.Count -eq 0) 'without -Base, the merge base of HEAD and the main branch is the base'

$noBase = Invoke-Handoff (New-HandoffCase 'ho-no-base' @{}) @() @('-MainBranch', 'nope')
Assert-That ($noBase.Exit -eq 1 -and ($noBase.Output -join ' ') -match 'No merge base') 'without -Base and without the main branch, the check fails with a message'

$scriptTest = Invoke-Handoff (New-HandoffCase 'ho-script-test' @{ScriptChange = $true })
Assert-That ($scriptTest.Exit -eq 1 -and $scriptTest.Output.Count -eq 1 -and
  $scriptTest.Output[0] -match 'script test \.claude/tests/flow-scripts\.tests\.ps1 fails: FAIL the sample check') `
  'a changed flow script runs its test script, and a failing test fails the check'

$fakeFormat = Join-Path $work 'fake-format.ps1'
Set-Content $fakeFormat @(
  'param([string]$Repo, [string[]]$Files)',
  'Add-Content (Join-Path $Repo ''src/b.cs'') ''// formatted''',
  '''Cleanup skipped: the test note.''')
$formatted = Invoke-Handoff (New-HandoffCase 'ho-format' @{}) @('-FormatScript', $fakeFormat)
Assert-That ($formatted.Exit -eq 1 -and ($formatted.Output -match 'The formatter changed src/b.cs').Count -eq 1 -and
  ($formatted.Output -match '^Formatter: Cleanup skipped').Count -eq 1 -and
  ($formatted.Output -match 'not the current backend tree').Count -eq 1) `
  'a file that the formatter changes fails, with its note and a stale Full run line'

# --- check-review-routing --------------------------------------------------------------------------------------
Write-Host 'check-review-routing'

$oldReview = Join-Path $work 'review-old-layout.md'
Set-Content $oldReview @(
  'Cycle: 1', 'Round: 1', '', '# Review', '', '## Open Findings', '',
  '| Severity | [CODE] | [SPEC] |', '|---|---|---|', '| BLOCKER | 0 | 0 |', '| MAJOR | 1 | 0 |', '| MINOR | 0 | 0 |', '',
  'Verdict: CHANGES_REQUESTED')
$oldLayout = Invoke-Script 'check-review-routing.ps1' @($oldReview)
Assert-That ($oldLayout.Exit -eq 1 -and ($oldLayout.Output -join ' ') -match 'review.md has no "## Open" section') `
  'a review.md without "## Open" fails with its line'

# The current layout: "## Open" with one block per open finding, "## Closed" with one line per closed finding.
function New-OpenReviewFile([string] $name, [hashtable] $change = @{}) {
  $path = Join-Path $work "$name.md"
  $heading = if ($change.BadHeading) { '### F1 — MAJOR — The save ignores the row version' } else { '### F1 MAJOR [CODE] The save ignores the row version' }
  $status = if ($change.OpenReopened) { '*   Status: REOPENED (2)' } else { '*   Status: NEW' }
  $majorCode = if ($change.WrongTable) { 2 } elseif ($change.NoOpen) { 0 } else { 1 }
  $verdict = if ($change.Approve) { 'APPROVE' } else { 'CHANGES_REQUESTED' }
  $lines = @(
    'Cycle: 1', 'Round: 2', 'Reviewed tree: 0123456789abcdef0123456789abcdef01234567', '', '# Review', '',
    '## Open', '') + $(if ($change.NoOpen) { @() } else { @($heading, $status, '*   Where: `src/a.cs:10`', '') }) + @(
    '## Closed', '', '*   F2 MINOR [SPEC] An old spec gap: REOPENED (2), then ACCEPTED (Artur, 2026-11-13).', '',
    '## Rounds', '', '*   Cycle 1, round 1: tree 0123456789abcdef0123456789abcdef01234567.', '',
    '## Summary', '', '| Severity | [CODE] | [SPEC] |', '|---|---|---|',
    '| BLOCKER | 0 | 0 |', "| MAJOR | $majorCode | 0 |", '| MINOR | 0 | 0 |', '', "Verdict: $verdict")
  Set-Content $path $lines
  return $path
}

$openClean = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open'))
Assert-That ($openClean.Exit -eq 0 -and ($openClean.Output -join '') -match '^executor code-fixes') `
  'an open [CODE] finding routes to the executor; a REOPENED (2) in ## Closed does not count'

$openReopened = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-reopened' @{ OpenReopened = $true }))
Assert-That ($openReopened.Exit -eq 0 -and ($openReopened.Output -join '') -match '^spec not-converging') `
  'a REOPENED (2) in ## Open routes to the spec session'

$wrongTable = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-table' @{ WrongTable = $true }))
Assert-That ($wrongTable.Exit -eq 1 -and ($wrongTable.Output -join ' ') -match 'summary row MAJOR') `
  'a summary table that differs from ## Open fails'

$badHeading = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-heading' @{ BadHeading = $true }))
Assert-That ($badHeading.Exit -eq 1 -and ($badHeading.Output -join ' ') -match 'is not .### F<n>') `
  'a finding heading in another format fails'

$badApprove = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-approve' @{ Approve = $true }))
Assert-That ($badApprove.Exit -eq 1 -and ($badApprove.Output -join ' ') -match 'APPROVE with 1 open') `
  'APPROVE with an open MAJOR finding fails'

$approved = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-routes-approve' @{ Approve = $true; NoOpen = $true }))
Assert-That ($approved.Exit -eq 0 -and ($approved.Output -join '') -match '^executor approved \(cycle 1, round 2\)') `
  'an APPROVE routes to the executor'

# --- commit-cycle ----------------------------------------------------------------------------------------------
Write-Host 'commit-cycle'

# A repo with the base commit on main, a branch develop at the base, and a bare remote origin with the base pushed.
# The change: an edited tooling file, a new docs file, an edited, a deleted, and a new code file. The folder holds the
# plan.md and the review.md of an approved cycle 1; the options change one fact for each failure case.
function New-CommitCase([string] $name, [hashtable] $change = @{}) {
  $repoDir = New-Repo "$name-repo"
  foreach ($dir in @('.claude', 'docs', 'src')) { New-Item -ItemType Directory -Force (Join-Path $repoDir $dir) | Out-Null }
  Set-Content (Join-Path $repoDir '.claude/x.ps1') '# x'
  Set-Content (Join-Path $repoDir 'docs/a.md') '# a'
  Set-Content (Join-Path $repoDir 'src/a.cs') 'class A {}'
  Set-Content (Join-Path $repoDir 'src/b.cs') 'class B {}'
  Save-Commit $repoDir 'base'
  git -C $repoDir branch develop
  $remote = Join-Path $work "$name-remote"
  git init -q --bare $remote
  git -C $repoDir remote add origin $remote
  git -C $repoDir push -q origin main
  $base = git -C $repoDir rev-parse HEAD

  Add-Content (Join-Path $repoDir '.claude/x.ps1') '# changed'
  if (-not $change.NoDocsChange) { Set-Content (Join-Path $repoDir 'docs/b.md') '# b' }
  Add-Content (Join-Path $repoDir 'src/a.cs') '// changed'
  if ($change.MoveFile) { git -C $repoDir mv src/b.cs src/d.cs } else { Remove-Item (Join-Path $repoDir 'src/b.cs') }
  Set-Content (Join-Path $repoDir 'src/c.cs') 'class C {}'

  $folder = Join-Path $work "$name-folder"
  New-Item -ItemType Directory -Force $folder | Out-Null
  $docsLine = if ($change.NoDocsCommit) { @() } else { @('Commit docs: feature(docs): described b') }
  $codeSubject = if ($change.BadSubject) { 'Feature(sample): Changed a' } else { 'feature(sample): changed a and b' }
  Set-Content (Join-Path $folder 'plan.md') (@('Cycle: 1', '', '# Plan', '', '### Cycle 1', '', 'Spec: **Feature A**, AC1.',
      'Commit tooling: feature(setup): changed x') + $docsLine + @("Commit code: $codeSubject", '',
      '- [x] Step 1: Change the code.', '    *   Files: `src/a.cs`', '', '#### Deviations', ''))
  $verdict = if ($change.ChangesRequested) { 'CHANGES_REQUESTED' } else { 'APPROVE' }
  $reviewCycle = if ($change.ReviewCycle) { $change.ReviewCycle } else { 1 }
  Set-Content (Join-Path $folder 'review.md') @("Cycle: $reviewCycle", 'Round: 1', "Reviewed tree: $(Get-TreeId $repoDir)", '',
    '## Open', '', '## Closed', '', '## Summary', '', '| Severity | [CODE] | [SPEC] |', '|---|---|---|',
    '| BLOCKER | 0 | 0 |', '| MAJOR | 0 | 0 |', '| MINOR | 0 | 0 |', '', "Verdict: $verdict")
  if ($change.LateChange) { Add-Content (Join-Path $repoDir 'src/a.cs') '// late' }
  return @{ Folder = $folder; Repo = $repoDir; Remote = $remote; Base = $base }
}

function Invoke-Commit($case) {
  return Invoke-Script 'commit-cycle.ps1' @($case.Folder, '-Repo', $case.Repo)
}

$commitCase = New-CommitCase 'cc-ok'
$committed = Invoke-Commit $commitCase
$subjects = @(git -C $commitCase.Repo log --format=%s "$($commitCase.Base)..HEAD")
Assert-That ($committed.Exit -eq 0 -and $subjects.Count -eq 3 -and $subjects[0] -eq 'feature(sample): changed a and b' -and
  $subjects[1] -eq 'feature(docs): described b' -and $subjects[2] -eq 'feature(setup): changed x') `
  'a commit group is committed in the order tooling, docs, code (newest first: code, docs, tooling)'
$shas = @(git -C $commitCase.Repo log --format=%H "$($commitCase.Base)..HEAD")
$expectedFiles = @('src/a.cs src/b.cs src/c.cs', 'docs/b.md', '.claude/x.ps1')
$isEachCommitRight = $shas.Count -eq 3
for ($i = 0; $i -lt $shas.Count -and $isEachCommitRight; $i++) {
  $files = (git -C $commitCase.Repo show --name-only --format= $shas[$i]) -join ' '
  if ($files -ne $expectedFiles[$i]) { $isEachCommitRight = $false }
}
Assert-That $isEachCommitRight 'each commit holds exactly the files of its group'
$isBodyClean = $shas.Count -eq 3
foreach ($sha in $shas) {
  $subject = (git -C $commitCase.Repo log -1 --format=%s $sha)
  if (((git -C $commitCase.Repo log -1 --format=%B $sha) -join "`n").Trim() -ne $subject) { $isBodyClean = $false }
  if ((git -C $commitCase.Repo log -1 --format=%an $sha) -ne 'Test') { $isBodyClean = $false }
}
Assert-That $isBodyClean 'each commit message is the subject only, and the author is the git identity of the repo'
Assert-That ((git -C $commitCase.Repo status --porcelain).Count -eq 0) 'the checkout is clean after the commits'
Assert-That ((git -C $commitCase.Remote rev-parse main) -eq $commitCase.Base) 'the script pushes nothing'

$beforeRerun = git -C $commitCase.Repo rev-parse HEAD
$rerun = Invoke-Commit $commitCase
Assert-That ($rerun.Exit -eq 0 -and $rerun.Output.Count -eq 0 -and (git -C $commitCase.Repo rev-parse HEAD) -eq $beforeRerun) `
  'a second run prints no SHA and makes no commit'

$renameCase = New-CommitCase 'cc-rename' @{ MoveFile = $true }
$renamed = Invoke-Commit $renameCase
$renameShas = @(git -C $renameCase.Repo log --format=%H "$($renameCase.Base)..HEAD")
$codeFiles = if ($renameShas.Count -eq 3) { (git -C $renameCase.Repo show --name-only --no-renames --format= $renameShas[0]) -join ' ' } else { '' }
Assert-That ($renamed.Exit -eq 0 -and $codeFiles -eq 'src/a.cs src/b.cs src/c.cs src/d.cs' -and
  (git -C $renameCase.Repo status --porcelain).Count -eq 0) `
  'a file moved with git mv is committed with its old and its new path, and the checkout is clean'

foreach ($case in @(
    @('cc-verdict', @{ ChangesRequested = $true }, 'no Verdict: APPROVE'),
    @('cc-cycle', @{ ReviewCycle = 2 }, 'is for cycle 2'),
    @('cc-late', @{ LateChange = $true }, 'is not the Reviewed tree'),
    @('cc-nodocscommit', @{ NoDocsCommit = $true }, 'docs/b.md needs a Commit docs line'),
    @('cc-nodocschange', @{ NoDocsChange = $true }, 'the Commit docs line has no changed files'),
    @('cc-subject', @{ BadSubject = $true }, "is not '<feature|bug>"))) {
  $failCase = New-CommitCase $case[0] $case[1]
  $headBefore = git -C $failCase.Repo rev-parse HEAD
  $statusBefore = (git -C $failCase.Repo status --porcelain) -join '|'
  $failed = Invoke-Commit $failCase
  Assert-That ($failed.Exit -eq 1 -and ($failed.Output -join ' ') -like "*$($case[2])*" -and
    (git -C $failCase.Repo rev-parse HEAD) -eq $headBefore -and ((git -C $failCase.Repo status --porcelain) -join '|') -eq $statusBefore) `
    "commit-cycle refuses with its reason and changes nothing: $($case[2])"
}

# --- fake tools for format-changed and run-area ------------------------------------------------------------------
# Each fake logs "<tool> <arguments> @ <folder name>" to FAKE_LOG. The fake npx prettier appends "// PRETTIER" to each
# file it gets. The fake dotnet answers like the Microsoft Testing Platform summary; FAKE_UNIT_FAIL makes the unit
# command fail, and FAKE_TOUCH names a file that the integration command changes.
$fakeBin = Join-Path $work 'bin'
New-Item -ItemType Directory -Force $fakeBin | Out-Null
$fakeLogLine = 'if ($env:FAKE_LOG) { Add-Content $env:FAKE_LOG "$tool $($args -join '' '') @ $(Split-Path -Leaf (Get-Location))" }'
Set-Content (Join-Path $fakeBin 'fake-npx.ps1') @(
  '$tool = ''npx''', $fakeLogLine,
  'if ($args[0] -eq ''prettier'') { foreach ($file in @($args | Where-Object { $_ -notlike ''-*'' -and $_ -ne ''prettier'' })) { if (Test-Path -LiteralPath $file) { Add-Content -LiteralPath $file ''// PRETTIER'' } }; exit 0 }',
  'if ($args[1] -eq ''test'') { Write-Output ''      Tests  3 passed (3)'' }',
  'exit 0')
Set-Content (Join-Path $fakeBin 'fake-npm.ps1') @(
  '$tool = ''npm''', $fakeLogLine,
  'if ($args[0] -eq ''test'') { Write-Output '' Tests  2 passed (2)'' }',
  'exit 0')
Set-Content (Join-Path $fakeBin 'fake-dotnet.ps1') @(
  '$tool = ''dotnet''',
  '$line = $args -join '' ''',
  '$kind = if ($args[0] -eq ''build'') { ''build'' } elseif ($line -match ''UnitTests'') { ''unit'' } else { ''integration'' }',
  'if ($env:FAKE_LOG) { Add-Content $env:FAKE_LOG "dotnet $kind" }',
  'if ($kind -eq ''unit'' -and $env:FAKE_UNIT_FAIL) { ''failed SampleTests.Unit_Fails (1ms)'', ''  total: 5'', ''  failed: 1'', ''  succeeded: 4''; exit 2 }',
  'if ($kind -eq ''build'' -and $env:FAKE_PROBE_LOCK) { try { Remove-Item -LiteralPath $env:FAKE_PROBE_LOCK -Force -ErrorAction Stop; Add-Content $env:FAKE_LOG ''lock delete worked'' } catch { Add-Content $env:FAKE_LOG ''lock delete blocked'' } }',
  'if ($kind -eq ''unit'') { ''  total: 5'', ''  failed: 0'', ''  succeeded: 5'' }',
  'if ($kind -eq ''integration'') { if ($env:FAKE_TOUCH) { Add-Content $env:FAKE_TOUCH ''// touched during the run'' }; ''  total: 7'', ''  failed: 0'', ''  succeeded: 7'' }',
  'exit 0')
foreach ($tool in 'npx', 'npm', 'dotnet') {
  "@pwsh -NoProfile -File ""%~dp0fake-$tool.ps1"" %*" | Set-Content (Join-Path $fakeBin "$tool.cmd") -Encoding ascii
}
$previousPath = $env:PATH
$env:PATH = "$fakeBin$([IO.Path]::PathSeparator)$env:PATH"

function Get-MarkCount([string] $path, [string] $mark) {
  ([regex]::Matches((Get-Content $path -Raw), [regex]::Escape($mark))).Count
}

# --- format-changed --------------------------------------------------------------------------------------------
Write-Host 'format-changed'
$formatRepo = New-Repo 'format-skip'
Set-Content (Join-Path $formatRepo '.gitignore') 'node_modules/'
New-Item -ItemType Directory -Force (Join-Path $formatRepo 'frontend/src'), (Join-Path $formatRepo 'frontend/node_modules') | Out-Null
foreach ($file in 'a', 'b') { Set-Content (Join-Path $formatRepo "frontend/src/$file.ts") "export const $file = 1;" }
Save-Commit $formatRepo 'base'
foreach ($file in 'a', 'b') { Add-Content (Join-Path $formatRepo "frontend/src/$file.ts") '// edited' }
$fileA = Join-Path $formatRepo 'frontend/src/a.ts'
$fileB = Join-Path $formatRepo 'frontend/src/b.ts'
[void](Invoke-Script 'format-changed.ps1' @('-Repo', $formatRepo, '-Files', 'frontend/src/a.ts'))
Assert-That ((Get-MarkCount $fileA '// PRETTIER') -eq 1) 'format-changed formats a given file'
[void](Invoke-Script 'format-changed.ps1' @('-Repo', $formatRepo, '-Files', 'frontend/src/a.ts'))
Assert-That ((Get-MarkCount $fileA '// PRETTIER') -eq 1) 'a second call at the same tree formats the file not again'
[void](Invoke-Script 'format-changed.ps1' @('-Repo', $formatRepo, '-Files', 'frontend/src/a.ts,frontend/src/b.ts'))
Assert-That ((Get-MarkCount $fileA '// PRETTIER') -eq 1 -and (Get-MarkCount $fileB '// PRETTIER') -eq 1) `
  'a call with one more file at the same tree formats only that file'
Set-Content (Join-Path $formatRepo 'frontend/src/c.ts') 'export const c = 1;'
[void](Invoke-Script 'format-changed.ps1' @('-Repo', $formatRepo, '-Files', 'frontend/src/a.ts'))
Assert-That ((Get-MarkCount $fileA '// PRETTIER') -eq 2) 'a call after an edit of another file formats the file again'

# --- run-area --------------------------------------------------------------------------------------------------
Write-Host 'run-area'
$areaFormat = Join-Path $work 'fake-area-format.ps1'
Set-Content $areaFormat @(
  'param([string]$Repo, [string[]]$Files)',
  'if ($env:FAKE_LOG) { Add-Content $env:FAKE_LOG "format $(@($Files | Sort-Object) -join '','')" }',
  'if ($Files.Count -gt 0) { Add-Content (Join-Path $Repo $Files[0]) ''// formatted'' }')

function New-AreaRepo([string] $name, [hashtable] $change = @{}) {
  $dir = New-Repo $name
  foreach ($folder in 'src', 'frontend', 'keycloak-theme') { New-Item -ItemType Directory -Force (Join-Path $dir $folder) | Out-Null }
  Set-Content (Join-Path $dir 'src/a.cs') 'class A {}'
  Set-Content (Join-Path $dir 'frontend/app.ts') 'export const a = 1;'
  Set-Content (Join-Path $dir 'keycloak-theme/theme.ts') 'export const t = 1;'
  if ($change.LongLine) { Set-Content (Join-Path $dir '.editorconfig') @('root = true', '[*]', 'max_line_length = 40') }
  Save-Commit $dir 'base'
  git -C $dir tag base
  Add-Content (Join-Path $dir 'src/a.cs') '// edit'
  Set-Content (Join-Path $dir 'src/b.cs') 'class B {}'
  if ($change.LongLine) { Add-Content (Join-Path $dir 'src/b.cs') ('// ' + ('x' * 50)) }
  Add-Content (Join-Path $dir 'frontend/app.ts') '// edit'
  if ($change.ThemeChange) { Add-Content (Join-Path $dir 'keycloak-theme/theme.ts') '// edit' }
  return $dir
}

function Invoke-Area([string] $repoDir, [string] $area, [hashtable] $environment = @{}) {
  $log = Join-Path $work "area-$([Guid]::NewGuid().ToString('N').Substring(0, 8)).log"
  New-Item -ItemType File $log | Out-Null
  $env:FAKE_LOG = $log
  foreach ($key in $environment.Keys) { Set-Item "Env:$key" $environment[$key] }
  try { $result = Invoke-Script 'run-area.ps1' @('-Area', $area, '-Repo', $repoDir, '-Base', 'base', '-FormatScript', $areaFormat) }
  finally {
    Remove-Item Env:FAKE_LOG -ErrorAction SilentlyContinue
    foreach ($key in $environment.Keys) { Remove-Item "Env:$key" -ErrorAction SilentlyContinue }
  }
  [pscustomobject]@{ Output = $result.Output; Exit = $result.Exit; Log = @(Get-Content $log); Last = @($result.Output)[-1] }
}

$areaRepo = New-AreaRepo 'ra-backend'
$backendRun = Invoke-Area $areaRepo 'backend'
$backendTreeAfter = Get-AreaTreeId $areaRepo 'backend'
Assert-That ($backendRun.Exit -eq 0 -and $backendRun.Last -eq
  "Full run backend: tree $backendTreeAfter; build, unit+architecture command, integration+API command -> 5 and 7 passed, 0 failed") `
  'a backend run passes with the Full run line of the tree after the format step'
Assert-That (($backendRun.Log -join '|') -eq 'format src/a.cs,src/b.cs|dotnet build|dotnet unit|dotnet integration') `
  'a backend run formats the changed backend files first, then runs the build, the unit, and the integration command'

$unitFail = Invoke-Area (New-AreaRepo 'ra-unit-fail') 'backend' @{ FAKE_UNIT_FAIL = '1' }
Assert-That ($unitFail.Exit -eq 1 -and $unitFail.Last -eq 'RUN FAILED backend: unit+architecture command failed' -and
  ($unitFail.Output -match 'failed SampleTests\.Unit_Fails').Count -ge 1 -and ($unitFail.Log -notcontains 'dotnet integration')) `
  'a failing unit command fails the run, names the failing test, and runs no integration command'

$longRun = Invoke-Area (New-AreaRepo 'ra-long' @{ LongLine = $true }) 'backend'
Assert-That ($longRun.Exit -eq 1 -and ($longRun.Output -contains 'src/b.cs:2 has 53 characters; max_line_length is 40.') -and
  $longRun.Last -like 'RUN FAILED backend:*' -and ($longRun.Log -notcontains 'dotnet build')) 'a long line fails the run before the build'

$touchRepo = New-AreaRepo 'ra-touch'
$touchRun = Invoke-Area $touchRepo 'backend' @{ FAKE_TOUCH = (Join-Path $touchRepo 'src/a.cs') }
Assert-That ($touchRun.Exit -eq 1 -and $touchRun.Last -eq 'RUN FAILED backend: the tree changed during the run') `
  'a file change during the tests fails the run'

$throwingFormat = Join-Path $work 'fake-throwing-format.ps1'
Set-Content $throwingFormat @('param([string]$Repo, [string[]]$Files)', 'throw ''the sample format failure''')
$throwRepo = New-AreaRepo 'ra-throw'
$env:FAKE_LOG = Join-Path $work 'ra-throw.log'
$throwRun = Invoke-Script 'run-area.ps1' @('-Area', 'backend', '-Repo', $throwRepo, '-Base', 'base', '-FormatScript', $throwingFormat)
Remove-Item Env:FAKE_LOG
Assert-That ($throwRun.Exit -eq 1 -and @($throwRun.Output)[-1] -eq 'RUN FAILED backend: the sample format failure') `
  'a step that throws ends the run with a RUN FAILED line'

$asciiRepo = New-AreaRepo 'ra-non-ascii'
[IO.File]::WriteAllText((Join-Path $asciiRepo "src/$([char]0x00DC)ber.cs"), 'class Uber {}')
$asciiRun = Invoke-Area $asciiRepo 'backend'
Assert-That ($asciiRun.Exit -eq 0 -and ($asciiRun.Log | Where-Object { $_ -like 'format *' }) -match "src/$([char]0x00DC)ber\.cs") `
  'a changed file with a non-ASCII name reaches the format script'

$frontendRun = Invoke-Area (New-AreaRepo 'ra-frontend') 'frontend'
$frontendCommands = @($frontendRun.Log | Where-Object { $_ -notlike 'format *' })
Assert-That ($frontendRun.Exit -eq 0 -and $frontendRun.Last -match '^Full run frontend: tree [0-9a-f]{40}; lint, ng build, ng test -> 3 passed, 0 failed$' -and
  ($frontendCommands -join '|') -eq 'npm run lint @ frontend|npx ng build @ frontend|npx ng test --watch=false @ frontend') `
  'a frontend run without a keycloak-theme change runs the 3 frontend commands'

$themeRun = Invoke-Area (New-AreaRepo 'ra-theme' @{ ThemeChange = $true }) 'frontend'
$themeCommands = @($themeRun.Log | Where-Object { $_ -notlike 'format *' })
Assert-That ($themeRun.Exit -eq 0 -and $themeRun.Last -match
  '^Full run frontend: tree [0-9a-f]{40}; lint, ng build, ng test, keycloak-theme lint, keycloak-theme test -> 3 and 2 passed, 0 failed$' -and
  ($themeCommands -join '|') -eq 'npm run lint @ frontend|npx ng build @ frontend|npx ng test --watch=false @ frontend|npm run lint @ keycloak-theme|npm test @ keycloak-theme') `
  'a frontend run with a keycloak-theme change also runs the 2 keycloak-theme commands'

function Get-RunLockPath([string] $repoDir, [string] $area) {
  [IO.Path]::GetFullPath((Join-Path (git -C $repoDir rev-parse --git-path claude-runs) "run-$area.lock"), $repoDir)
}

Assert-That (-not (Test-Path (Get-RunLockPath $areaRepo 'backend'))) 'the lock of a passed run is gone'
$unitFailRepo = New-AreaRepo 'ra-lock-fail'
$null = Invoke-Area $unitFailRepo 'backend' @{ FAKE_UNIT_FAIL = '1' }
Assert-That (-not (Test-Path (Get-RunLockPath $unitFailRepo 'backend'))) 'the lock of a failed run is gone'

$liveRepo = New-AreaRepo 'ra-lock-live'
$holder = Start-Process pwsh -ArgumentList '-NoProfile', '-Command', 'Start-Sleep -Seconds 120' -PassThru
$liveLock = Get-RunLockPath $liveRepo 'backend'
New-Item -ItemType Directory -Force (Split-Path $liveLock) | Out-Null
$liveStream = [IO.File]::Open($liveLock, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$liveBytes = [Text.Encoding]::ASCII.GetBytes("$($holder.Id)")
$liveStream.Write($liveBytes, 0, $liveBytes.Length)
$liveStream.Flush()
$liveRun = Invoke-Area $liveRepo 'backend'
$liveStream.Dispose()
Stop-Process -Id $holder.Id -Force -ErrorAction SilentlyContinue
Assert-That ($liveRun.Exit -eq 1 -and $liveRun.Last -eq "RUN FAILED backend: another run of backend is active (pid $($holder.Id))" -and
  (Test-Path $liveLock) -and ($liveRun.Log -notcontains 'dotnet build')) `
  'a run while another run holds the lock file open fails at once, names the holder, and leaves the lock'

$deadRepo = New-AreaRepo 'ra-lock-dead'
$deadProcess = Start-Process pwsh -ArgumentList '-NoProfile', '-Command', 'exit 0' -PassThru
$deadProcess.WaitForExit()
$deadLock = Get-RunLockPath $deadRepo 'backend'
New-Item -ItemType Directory -Force (Split-Path $deadLock) | Out-Null
Set-Content $deadLock "$($deadProcess.Id)"
$deadRun = Invoke-Area $deadRepo 'backend'
Assert-That ($deadRun.Exit -eq 0 -and $deadRun.Last -like 'Full run backend:*' -and -not (Test-Path $deadLock)) `
  'a lock file that no run holds open is stale: it does not block and is gone after the run'

$probeRepo = New-AreaRepo 'ra-lock-probe'
$probeRun = Invoke-Area $probeRepo 'backend' @{ FAKE_PROBE_LOCK = (Get-RunLockPath $probeRepo 'backend') }
Assert-That ($probeRun.Exit -eq 0 -and $probeRun.Log -contains 'lock delete blocked' -and $probeRun.Log -notcontains 'lock delete worked') `
  'a run holds its lock file open, so no other process can delete it while the run lives'

$emptyRepo = New-AreaRepo 'ra-lock-empty'
$emptyLock = Get-RunLockPath $emptyRepo 'backend'
New-Item -ItemType Directory -Force (Split-Path $emptyLock) | Out-Null
Set-Content $emptyLock ''
$emptyRun = Invoke-Area $emptyRepo 'backend'
Assert-That ($emptyRun.Exit -eq 0 -and $emptyRun.Last -like 'Full run backend:*' -and -not (Test-Path $emptyLock)) `
  'an empty lock file that no run holds open does not block'

$staleOpenRepo = New-AreaRepo 'ra-lock-stale-open'
$staleOpenLock = Get-RunLockPath $staleOpenRepo 'backend'
New-Item -ItemType Directory -Force (Split-Path $staleOpenLock) | Out-Null
$staleOpenStream = [IO.File]::Open($staleOpenLock, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$staleOpenBytes = [Text.Encoding]::ASCII.GetBytes("$($deadProcess.Id)")
$staleOpenStream.Write($staleOpenBytes, 0, $staleOpenBytes.Length)
$staleOpenStream.Flush()
$staleOpenRun = Invoke-Area $staleOpenRepo 'backend'
$staleOpenStream.Dispose()
Assert-That ($staleOpenRun.Exit -eq 1 -and $staleOpenRun.Last -like 'RUN FAILED backend: another run of backend is active*' -and
  (Test-Path $staleOpenLock) -and ($staleOpenRun.Log -notcontains 'dotnet build')) `
  'a lock with the ID of a dead process that another run holds open is left alone and the run fails'

$openRepo = New-AreaRepo 'ra-lock-open'
$openLock = Get-RunLockPath $openRepo 'backend'
New-Item -ItemType Directory -Force (Split-Path $openLock) | Out-Null
$openStream = [IO.File]::Open($openLock, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
$openRun = Invoke-Area $openRepo 'backend'
$openStream.Dispose()
Assert-That ($openRun.Exit -eq 1 -and $openRun.Last -like 'RUN FAILED backend: another run of backend is active*' -and
  (Test-Path $openLock) -and ($openRun.Log -notcontains 'dotnet build')) `
  'a run while another run holds the lock file open with an exclusive create fails at once and leaves the lock'

$env:PATH = $previousPath

Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
if ($failures.Count -gt 0) {
  Write-Host "`n$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host "`nAll checks passed."
