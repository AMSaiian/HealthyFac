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
#   a Proof: line or a "done" log in the Deviations; a formatter change;
#   the default base (merge base with the main branch); a failing script test of a changed flow script; an old
#   "Full run: tree" line and an old plan-wide "## Deviations" count for nothing; the browser flow lines (a pass line
#   without seen:, a pass line with a caveat, a transient fail without retried: fail; a retry note and step prose pass).
# - check-review-routing: the "## Open" layout routes, counts REOPENED only in ## Open, and fails on a wrong table, a
#   wrong heading, APPROVE with an open MAJOR, or a review.md without "## Open".
# - format-changed: every call formats every given file again (Prettier and the cleanup); it writes no state file.
# - run-area: the backend order (format, build, unit, integration) and the Full run line with the tree after the
#   format step; a named index argument, a failing unit command, and a change during the run fail it; the frontend commands with
#   and without a keycloak-theme change.
# - commit-cycle: an approved cycle is committed as one commit for each group (tooling, docs, code) with the subject
#   only, nothing is pushed, a rerun makes no commit; each refusal (no APPROVE, another cycle, a changed tree, a changed
#   file without a Commit line, a Commit line without a changed file, a bad subject) changes nothing; a Fixup entry is
#   committed with --fixup and folded by the autosquash (same tree, no fixup! commit); a fixup at or below the reviewed
#   SHA stays; a Fixup subject with 0 or 2 matches, a conflict, -Verify, a changed tree, and -Squash.
# - plan-sections and check-plan: the Fixup lines, the inline fix entries, the reviewed mark, and each Fixup problem.
# - check-review-routing: a Scope: branch review routes branch approved and branch findings.
# Red: run it with -ScriptsDir <folder of empty stubs> to see each case fail first.
# Sections: tree-id, check-plan, plan-sections, check-handoff, check-review-routing, commit-cycle, format-changed,
# run-area. -Only <names> (comma-separated) runs the shared setup and only the named sections; an unknown name exits 1
# before any check. The last line names the sections that ran. Without -Only, every section runs.
# Run:  pwsh .claude/tests/flow-scripts.tests.ps1 [-ScriptsDir <folder>] [-Only <names>]
# It exits 1 when a check fails.
param([string]$ScriptsDir = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent)), [string]$Only = '')

$SectionNames = @('tree-id', 'check-plan', 'plan-sections', 'check-handoff', 'check-review-routing', 'commit-cycle',
  'format-changed', 'run-area', 'run-tests')
$onlyNames = @($Only -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
foreach ($name in $onlyNames) {
  if ($name -notin $SectionNames) {
    Write-Host "unknown section '$name'; sections: $($SectionNames -join ', ')."
    exit 1
  }
}
$ranSections = [Collections.Generic.List[string]]::new()

# True when the section runs: always without -Only, else when one of the names is in -Only. -Quiet leaves the section
# out of the list on the last line (the shared setup of several sections).
function Test-Section([string[]] $Names, [switch] $Quiet) {
  $isSelected = $onlyNames.Count -eq 0 -or @($Names | Where-Object { $_ -in $onlyNames }).Count -gt 0
  if ($isSelected -and -not $Quiet) { $ranSections.Add($Names[0]) }
  return $isSelected
}

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

# Output holds the lines of the script without its base notes ("Note: origin/<branch> not found; ..."), which the
# default base helper writes to the host; Notes holds those lines.
function Invoke-Script([string] $name, [string[]] $arguments) {
  $output = & pwsh -NoProfile -File (Join-Path $ScriptsDir $name) @arguments 2>&1
  $lines = @($output | ForEach-Object { "$_" })
  return [pscustomobject]@{
    Output = @($lines | Where-Object { $_ -notlike 'Note: origin/*' }); Exit = $LASTEXITCODE
    Notes = @($lines | Where-Object { $_ -like 'Note: origin/*' }) }
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

function Get-AreaTreeId([string] $repo, [string] $area) {
  $result = Invoke-Script 'tree-id.ps1' @('-Repo', $repo, '-Area', $area)
  return ($result.Output -join '').Trim()
}

# --- tree-id ---------------------------------------------------------------------------------------------------
if (Test-Section 'tree-id') {
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

# The tooling area: .claude/, CLAUDE.md, and scripts/ only (Step 86).
$toolRepo = New-Repo 'tree-tooling'
foreach ($folder in 'src', '.claude', 'scripts') { New-Item -ItemType Directory -Force (Join-Path $toolRepo $folder) | Out-Null }
Set-Content (Join-Path $toolRepo 'src/a.cs') 'class A {}'
Set-Content (Join-Path $toolRepo '.claude/tool.ps1') 'Write-Host 1'
Set-Content (Join-Path $toolRepo 'scripts/verify.ps1') 'Write-Host 1'
Set-Content (Join-Path $toolRepo 'CLAUDE.md') 'rules'
Save-Commit $toolRepo 'base'
$toolingBefore = Get-AreaTreeId $toolRepo 'tooling'
$backendBefore = Get-AreaTreeId $toolRepo 'backend'
Add-Content (Join-Path $toolRepo 'src/a.cs') '// edit'
Assert-That ($toolingBefore -match '^[0-9a-f]{40}$' -and (Get-AreaTreeId $toolRepo 'tooling') -eq $toolingBefore) `
  'the tooling tree ID does not change with a backend file'
Assert-That ((Get-AreaTreeId $toolRepo 'backend') -ne $backendBefore) 'the backend tree ID changes with a backend file'
$toolingMark = Get-AreaTreeId $toolRepo 'tooling'
Add-Content (Join-Path $toolRepo 'scripts/verify.ps1') '# edit'
Assert-That ((Get-AreaTreeId $toolRepo 'tooling') -ne $toolingMark) 'the tooling tree ID changes with scripts/verify.ps1'
$backendMid = Get-AreaTreeId $toolRepo 'backend'
foreach ($toolFile in '.claude/tool.ps1', 'CLAUDE.md') {
  $toolingMark = Get-AreaTreeId $toolRepo 'tooling'
  Add-Content (Join-Path $toolRepo $toolFile) '# edit'
  Assert-That ((Get-AreaTreeId $toolRepo 'tooling') -ne $toolingMark) "the tooling tree ID changes with $toolFile"
}
Assert-That ((Get-AreaTreeId $toolRepo 'backend') -eq $backendMid) 'the backend tree ID does not change with .claude/ or CLAUDE.md'

# The -Only switch, run as child processes (the known section has no switch test, so the child does not recurse).
$onlyOutput = @(& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'flow-scripts.tests.ps1') -ScriptsDir $ScriptsDir -Only plan-sections 2>&1 |
    ForEach-Object { "$_" })
Assert-That ($LASTEXITCODE -eq 0 -and $onlyOutput[-1] -eq 'All checks passed (plan-sections).' -and
  -not ($onlyOutput -contains 'tree-id')) '-Only with one known name runs only that section and names it on the last line'
$unknownOutput = @(& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'flow-scripts.tests.ps1') -Only nosuchsection 2>&1 |
    ForEach-Object { "$_" })
Assert-That ($LASTEXITCODE -eq 1 -and $unknownOutput.Count -eq 1 -and $unknownOutput[0] -like "unknown section 'nosuchsection'; sections: *" -and
  -not ($unknownOutput -match '^\s+ok ')) '-Only with an unknown name fails before any check'

}

# --- check-plan ------------------------------------------------------------------------------------------------
if (Test-Section 'check-plan') {
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
  $planLines = @($firstLine)
  if ($change.HeaderLines) { $planLines += $change.HeaderLines }
  $planLines += '', '# Plan', '', '### Cycle 1', ''
  $planLines += $specLine1
  $planLines += $cycle1Commits
  if ($change.Cycle1Lines) { $planLines += $change.Cycle1Lines }
  $planLines += '', '- [x] Step 1: Do the first thing.', '    *   Files: `src/a.cs`', '', '#### Deviations', ''
  $planLines += '### Cycle 2', ''
  if (-not $change.NoCycle2Spec) { $planLines += $specLine2 }
  $planLines += $cycle2Commits
  if ($change.Cycle2Lines) { $planLines += $change.Cycle2Lines }
  $planLines += ''
  $step2Mark = if ($change.Step2Mark) { $change.Step2Mark } else { ' ' }
  $planLines += "- [$step2Mark] $step2", "    *   Files: $step2Files"
  if ($change.Step2Fixup) { $planLines += "    *   Fixup: $($change.Step2Fixup)" }
  if ($change.Step2Look) { $planLines += "    *   Look: $($change.Step2Look)" }
  if ($change.Step2Verify) { $planLines += "    *   Verify: $($change.Step2Verify)" }
  if ($change.Step3Files) { $planLines += '', '- [ ] Step 3: Do the third thing.', "    *   Files: $($change.Step3Files)" }
  if ($change.Step3Fixup) { $planLines += "    *   Fixup: $($change.Step3Fixup)" }
  $planLines += ''
  if (-not $change.NoCycle2Deviations) { $planLines += '#### Deviations', '' }
  if ($change.DeviationLines) { $planLines += $change.DeviationLines }
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

function New-FixupRepo([string] $name) {
  $dir = New-Repo $name
  Set-Content (Join-Path $dir 'a.txt') 'a'
  Save-Commit $dir 'base'
  git -C $dir branch develop
  git -C $dir checkout -q -b feature
  Add-Content (Join-Path $dir 'a.txt') 'b'
  Save-Commit $dir 'feature(sample): added a'
  return $dir
}

function Invoke-PlanInRepo([string] $name, [hashtable] $change, [string] $repo) {
  return Invoke-Script 'check-plan.ps1' @((New-PlanFolder $name $change), '-Repo', $repo)
}

$fixupRepo = New-FixupRepo 'fixup-repo'
$fixupOk = Invoke-PlanInRepo 'plan-fixup-ok' @{ Step2Fixup = 'feature(sample): added a' } $fixupRepo
Assert-That ($fixupOk.Exit -eq 0 -and $fixupOk.Output.Count -eq 0) 'a Fixup step whose subject matches one commit passes'

$fixupNone = Invoke-PlanInRepo 'plan-fixup-none' @{ Step2Fixup = 'feature(sample): added z' } $fixupRepo
Assert-That ($fixupNone.Exit -eq 1 -and $fixupNone.Output.Count -eq 1 -and
  $fixupNone.Output[0] -like "Cycle 2: Step 2: the Fixup subject 'feature(sample): added z' matches no commit of *..HEAD.") `
  'a Fixup subject that matches no commit fails with its line'

$fixupTwice = New-FixupRepo 'fixup-repo-twice'
Add-Content (Join-Path $fixupTwice 'a.txt') 'c'
Save-Commit $fixupTwice 'feature(sample): added a'
$fixupMany = Invoke-PlanInRepo 'plan-fixup-many' @{ Step2Fixup = 'feature(sample): added a' } $fixupTwice
Assert-That ($fixupMany.Exit -eq 1 -and $fixupMany.Output.Count -eq 1 -and
  $fixupMany.Output[0] -like "Cycle 2: Step 2: the Fixup subject 'feature(sample): added a' matches 2 commits of *..HEAD.") `
  'a Fixup subject that matches 2 commits fails with its line'

$fixupShared = Invoke-PlanInRepo 'plan-fixup-shared' @{
  Step2Fixup = 'feature(sample): added a'; Step2Files = '`src/b.cs`'; Step3Files = '`src/b.cs`' } $fixupRepo
Assert-That ($fixupShared.Exit -eq 1 -and $fixupShared.Output.Count -eq 1 -and
  $fixupShared.Output[0] -eq 'Cycle 2: Step 2: src/b.cs is in a Fixup step and in Step 3 without Fixup.') `
  'a path in a Fixup step and in a step without Fixup fails with its line'

$fixupOtherFile = Invoke-PlanInRepo 'plan-fixup-other' @{
  Step2Fixup = 'feature(sample): added a'; Step2Files = '`src/b.cs`'; Step3Files = '`src/c.cs`' } $fixupRepo
Assert-That ($fixupOtherFile.Exit -eq 0 -and $fixupOtherFile.Output.Count -eq 0) 'a Fixup step and a plain step with different files pass'

$headSha = git -C $fixupRepo rev-parse HEAD
$markOk = Invoke-PlanInRepo 'plan-mark-ok' @{ HeaderLines = @("Reviewed by Artur: $headSha") } $fixupRepo
Assert-That ($markOk.Exit -eq 0 -and $markOk.Output.Count -eq 0) 'a Reviewed by Artur SHA that is an ancestor of HEAD passes'
$otherSha = '0123456789012345678901234567890123456789'
$markBad = Invoke-PlanInRepo 'plan-mark-bad' @{ HeaderLines = @("Reviewed by Artur: $otherSha") } $fixupRepo
Assert-That ($markBad.Exit -eq 1 -and $markBad.Output.Count -eq 1 -and
  $markBad.Output[0] -eq "plan.md: the Reviewed by Artur SHA $otherSha is no ancestor of HEAD.") `
  'a Reviewed by Artur SHA that is no ancestor of HEAD fails with its line'

# A cancelled Fixup step is no fixup entry (Step 68).
$cancelledPlan = Invoke-PlanInRepo 'plan-fixup-cancelled' @{
  Step2Mark = '~'; Step2Fixup = 'feature(sample): added a'; Step2Files = '`src/b.cs`'; Step3Files = '`src/b.cs`' } $fixupRepo
Assert-That ($cancelledPlan.Exit -eq 0 -and $cancelledPlan.Output.Count -eq 0) `
  'a cancelled Fixup step and a plain replacing step with the same file report no shared path'

# A cycle of only fixup steps needs no Commit line. Step 62.
$fixupOnly = Invoke-PlanInRepo 'plan-fixup-only' @{ Step2Fixup = 'feature(sample): added a'; Cycle2Commits = @() } $fixupRepo
Assert-That ($fixupOnly.Exit -eq 0 -and $fixupOnly.Output.Count -eq 0) 'a cycle of only Fixup steps without a Commit line passes'
$fixupBrowser = Invoke-PlanInRepo 'plan-fixup-browser' @{
  Step2Fixup = 'feature(sample): added a'; Step3Files = 'none'; Cycle2Commits = @() } $fixupRepo
Assert-That ($fixupBrowser.Exit -eq 0 -and $fixupBrowser.Output.Count -eq 0) 'a Fixup step and a step with Files: none pass without a Commit line'
$mainRepo = New-Repo 'fixup-origin-main'
Set-Content (Join-Path $mainRepo 'a.txt') 'a'
Save-Commit $mainRepo 'base'
git -C $mainRepo update-ref refs/remotes/origin/main HEAD
git -C $mainRepo checkout -q -b feature
Add-Content (Join-Path $mainRepo 'a.txt') 'b'
Save-Commit $mainRepo 'feature(sample): added a'
git -C $mainRepo branch -f main HEAD
$mainBranchRun = Invoke-Script 'check-plan.ps1' @((New-PlanFolder 'plan-main-branch' @{ Step2Fixup = 'feature(sample): added a'; Cycle2Commits = @() }),
  '-Repo', $mainRepo, '-MainBranch', 'main')
Assert-That ($mainBranchRun.Exit -eq 0 -and $mainBranchRun.Output.Count -eq 0 -and $mainBranchRun.Notes.Count -eq 0) `
  'check-plan -MainBranch main takes the base from origin/main, not from the local main'
$fixupAndPlain = Invoke-PlanInRepo 'plan-fixup-plain-nocommit' @{
  Step2Fixup = 'feature(sample): added a'; Step3Files = '`src/c.cs`'; Cycle2Commits = @() } $fixupRepo
Assert-That ($fixupAndPlain.Exit -eq 1 -and $fixupAndPlain.Output.Count -eq 1 -and $fixupAndPlain.Output[0] -eq 'Cycle 2: no Commit line.') `
  'a cycle with a Fixup step and a plain step without a Commit line fails'
$fixupBadCommit = Invoke-PlanInRepo 'plan-fixup-bad-commit' @{
  Step2Fixup = 'feature(sample): added a'; Cycle2Commits = @('Commit code: Feature(sample): Bad') } $fixupRepo
Assert-That ($fixupBadCommit.Exit -eq 1 -and $fixupBadCommit.Output.Count -eq 1 -and $fixupBadCommit.Output[0] -like "*is not '<feature|bug>*") `
  'a fixup-only cycle with a bad Commit line still reports the Commit line problem'

# A path claimed by two fixup entries. Step 64.
$fixupTwoSubjects = New-FixupRepo 'fixup-repo-two-subjects'
Add-Content (Join-Path $fixupTwoSubjects 'a.txt') 'z'
Save-Commit $fixupTwoSubjects 'feature(sample): added z'
$twoEntries = Invoke-PlanInRepo 'plan-fixup-two-entries' @{
  Step2Fixup = 'feature(sample): added a'; Step2Files = '`src/b.cs`'; Step3Files = '`src/b.cs`'
  Step3Fixup = 'feature(sample): added z'; Cycle2Commits = @() } $fixupTwoSubjects
Assert-That ($twoEntries.Exit -eq 1 -and $twoEntries.Output.Count -eq 1 -and
  $twoEntries.Output[0] -eq "Cycle 2: src/b.cs is in the Fixup entries of 'feature(sample): added a' and 'feature(sample): added z'.") `
  'a path in two Fixup steps with different subjects fails with its line'
$sameEntries = Invoke-PlanInRepo 'plan-fixup-same-entries' @{
  Step2Fixup = 'feature(sample): added a'; Step2Files = '`src/b.cs`'; Step3Files = '`src/b.cs`'
  Step3Fixup = 'feature(sample): added a'; Cycle2Commits = @() } $fixupTwoSubjects
Assert-That ($sameEntries.Exit -eq 0 -and $sameEntries.Output.Count -eq 0) 'two Fixup steps with the same subject and the same path pass'
$inlineVsPlain = Invoke-PlanInRepo 'plan-inline-plain' @{
  Step3Files = '`src/c.cs`'
  DeviationLines = @('*   Inline fix 1: changed c. Fixup: feature(sample): added a; files: `src/c.cs`') } $fixupTwoSubjects
Assert-That ($inlineVsPlain.Exit -eq 1 -and $inlineVsPlain.Output.Count -eq 1 -and
  $inlineVsPlain.Output[0] -eq 'Cycle 2: src/c.cs is in an inline fix with Fixup and in Step 3 without Fixup.') `
  'a path in an inline fix with Fixup and in a plain step fails with its line'
$inlineUnknown = Invoke-PlanInRepo 'plan-inline-unknown' @{
  DeviationLines = @('*   Inline fix 1: changed c. Fixup: feature(sample): added z; files: `src/c.cs`') } $fixupRepo
Assert-That ($inlineUnknown.Exit -eq 1 -and $inlineUnknown.Output.Count -eq 1 -and
  $inlineUnknown.Output[0] -like "Cycle 2: the Fixup subject 'feature(sample): added z' matches no commit of *..HEAD.") `
  'an inline fix with an unknown Fixup subject fails with its line'
$inlineVsStep = Invoke-PlanInRepo 'plan-inline-step' @{
  Step2Fixup = 'feature(sample): added a'; Step2Files = '`src/b.cs`'; Cycle2Commits = @()
  DeviationLines = @('*   Inline fix 1: changed b. Fixup: feature(sample): added z; files: `src/b.cs`') } $fixupTwoSubjects
Assert-That ($inlineVsStep.Exit -eq 1 -and $inlineVsStep.Output.Count -eq 1 -and
  $inlineVsStep.Output[0] -eq "Cycle 2: src/b.cs is in the Fixup entries of 'feature(sample): added a' and 'feature(sample): added z'.") `
  'a path in a Fixup step and in an inline fix with another subject fails with its line'


}

# --- plan-sections ---------------------------------------------------------------------------------------------
if (Test-Section 'plan-sections') {
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

foreach ($case in @(
    @('- [!] Step 5: Browser check of the page. WAITING: no Chrome; retried: once', 'waiting'),
    @('- [!] Step 5: Browser check of the page. BLOCKED: flow 2 failed', 'blocked'),
    @('- [!] Step 5: Run the tests. WAITING: step 4', $null),
    @('- [ ] Step 5: Browser check of the page.', $null))) {
  $mark = Get-BrowserCheckMark -Step ([pscustomobject]@{ Mark = $case[0].Substring(3, 1); Number = '5'; Lines = @($case[0]) })
  Assert-That ($mark -eq $case[1]) "Get-BrowserCheckMark returns '$($case[1])' for '$($case[0])'"
}

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

$fixupSteps = @(
  [pscustomobject]@{ Number = '3'; Mark = ' '; Lines = @('- [ ] Step 3: Fix a.', '    *   Files: `a/b.cs`, `a/c.cs` (new).',
      '    *   Fixup: feature(setup): added a') },
  [pscustomobject]@{ Number = '4'; Mark = ' '; Lines = @('- [ ] Step 4: Plain.', '    *   Files: `d.cs`.') })
$fixups = @(Get-StepFixups $fixupSteps)
Assert-That ($fixups.Count -eq 1 -and $fixups[0].Step -eq '3' -and $fixups[0].Subject -eq 'feature(setup): added a' -and
  @($fixups[0].Files).Count -eq 2 -and $fixups[0].Files[0] -eq 'a/b.cs' -and $fixups[0].Files[1] -eq 'a/c.cs') `
  'Get-StepFixups returns the step, subject, and both files of a Fixup step and none for a plain step'
$inlineFixups = @(Get-InlineFixups @('*   Inline fix 1: changed x. Fixup: feature(setup): added a; files: `a/b.cs`, `c.cs`',
    '*   Inline fix 2: changed y.', 'other line'))
Assert-That ($inlineFixups.Count -eq 1 -and $inlineFixups[0].Subject -eq 'feature(setup): added a' -and
  @($inlineFixups[0].Files).Count -eq 2 -and $inlineFixups[0].Files[1] -eq 'c.cs') `
  'Get-InlineFixups returns the subject and files of an inline fix entry with Fixup:'
$wrappedFixups = @(Get-InlineFixups @('*   Inline fix 1: changed x and y in a long entry that wraps', '    over the second line. Fixup: feature(setup): added a;',
    '    files: `a/b.cs`, `c.cs`', '*   Inline fix 2: changed z. Fixup: feature(setup): added b; files: `d.cs`', 'continuation of fix 2 only'))
Assert-That ($wrappedFixups.Count -eq 2 -and $wrappedFixups[0].Subject -eq 'feature(setup): added a' -and
  @($wrappedFixups[0].Files).Count -eq 2 -and $wrappedFixups[0].Files[1] -eq 'c.cs' -and
  $wrappedFixups[1].Subject -eq 'feature(setup): added b' -and @($wrappedFixups[1].Files).Count -eq 1) `
  'Get-InlineFixups joins the continuation lines of an entry, and a continuation never joins the next entry'
$cancelledSteps = @([pscustomobject]@{ Number = '3'; Mark = '~'; Lines = @('- [~] Step 3: Cancelled.', '    *   Files: `a.cs`',
      '    *   Fixup: feature(setup): added a') })
Assert-That (@(Get-StepFixups $cancelledSteps).Count -eq 0) 'Get-StepFixups skips a cancelled step'
$subjectEntries = @([pscustomobject]@{ Step = '3'; Subject = 'feature(setup): added a'; Files = @('x.cs') },
  [pscustomobject]@{ Subject = 'feature(setup): added z'; Files = @('y.cs') },
  [pscustomobject]@{ Subject = 'feature(setup): added b'; Files = @('z.cs') })
$subjectProblems = @(Get-FixupSubjectProblems -Entries $subjectEntries -Base 'abc1234' `
    -Subjects @('feature(setup): added a', 'feature(setup): added b', 'feature(setup): added b'))
Assert-That ($subjectProblems.Count -eq 2 -and
  $subjectProblems[0] -eq "the Fixup subject 'feature(setup): added z' matches no commit of abc1234..HEAD." -and
  $subjectProblems[1] -eq "the Fixup subject 'feature(setup): added b' matches 2 commits of abc1234..HEAD.") `
  'Get-FixupSubjectProblems reports an entry with 0 or 2 matching subjects and none for 1 match'
$stepSubjectProblems = @(Get-FixupSubjectProblems -Entries @($subjectEntries[0]) -Base 'abc1234' -Subjects @())
Assert-That ($stepSubjectProblems.Count -eq 1 -and
  $stepSubjectProblems[0] -eq "Step 3: the Fixup subject 'feature(setup): added a' matches no commit of abc1234..HEAD.") `
  'Get-FixupSubjectProblems names the step of a Fixup step entry'
$pathSteps = @(
  [pscustomobject]@{ Number = '3'; Mark = ' '; Lines = @('- [ ] Step 3: Fix a.', '    *   Files: `a/b.cs`', '    *   Fixup: feature(setup): added a') },
  [pscustomobject]@{ Number = '4'; Mark = ' '; Lines = @('- [ ] Step 4: Plain.', '    *   Files: `a/b.cs`.') })
$pathProblems = @(Get-FixupPathProblems 2 $pathSteps @())
Assert-That ($pathProblems.Count -eq 1 -and $pathProblems[0] -eq 'Cycle 2: Step 3: a/b.cs is in a Fixup step and in Step 4 without Fixup.') `
  'Get-FixupPathProblems reports a path of a Fixup step that a plain step lists'
$onlySteps = @(
  [pscustomobject]@{ Number = '3'; Mark = ' '; Lines = @('- [ ] Step 3: Fix a.', '    *   Files: `a.cs`', '    *   Fixup: feature(setup): added a') },
  [pscustomobject]@{ Number = '4'; Mark = ' '; Lines = @('- [ ] Step 4: Browser check.', '    *   Files: none') },
  [pscustomobject]@{ Number = '5'; Mark = ' '; Lines = @('- [ ] Step 5: Browser check list.') })
Assert-That (Test-FixupOnlyCycle $onlySteps) 'Test-FixupOnlyCycle is true for a Fixup step and steps without files'
$plainSteps = $onlySteps + @([pscustomobject]@{ Number = '6'; Mark = ' '; Lines = @('- [ ] Step 6: Plain.', '    *   Files: `d.cs`') })
Assert-That (-not (Test-FixupOnlyCycle $plainSteps)) 'Test-FixupOnlyCycle is false when a step with files has no Fixup line'
Assert-That (-not (Test-FixupOnlyCycle @($onlySteps[1], $onlySteps[2]))) 'Test-FixupOnlyCycle is false without a fixup entry'
Assert-That (Test-FixupOnlyCycle @($onlySteps[1]) @([pscustomobject]@{ Subject = 'feature(setup): added a'; Files = @('x.cs') })) `
  'Test-FixupOnlyCycle counts an inline fixup entry'
$markSha = 'a' * 40
Assert-That ((Get-ReviewedMark @('Cycle: 1', "Reviewed by Artur: $markSha")) -eq $markSha) 'Get-ReviewedMark reads the SHA from the header'
Assert-That ($null -eq (Get-ReviewedMark (@('Cycle: 1') + (1..25 | ForEach-Object { '' }) + @("Reviewed by Artur: $markSha")))) `
  'Get-ReviewedMark ignores a mark below line 20'

}

# --- check-handoff ---------------------------------------------------------------------------------------------
if (Test-Section 'check-handoff') {
Write-Host 'check-handoff'

# Stores a run result like run-area.ps1 (mode full) or run-tests.ps1 (mode affected) does, in the git directory of the repo.
function Write-CaseResult([string] $repoDir, [string] $mode, [string] $area, [string] $tree, [string] $status = 'pass') {
  $folder = Join-Path $repoDir '.git/claude-runs'
  New-Item -ItemType Directory -Force $folder | Out-Null
  [ordered]@{ kind = if ($mode -eq 'full') { 'Full run' } else { 'Affected run' }; area = $area; tree = $tree; status = $status
    line = "$area run"; finished = '2026-11-26T09:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $folder "result-$area-$mode.json")
}

# A plan with "#### Deviations" in the cycle, and the stored results of the runs (the RunMode change picks full or affected).
function New-HandoffCase([string] $name, [hashtable] $change = @{}) {
  $repoDir = New-Repo "$name-repo"
  New-Item -ItemType Directory -Force (Join-Path $repoDir 'src') | Out-Null
  Set-Content (Join-Path $repoDir 'src/a.cs') 'class A {}'
  Save-Commit $repoDir 'base'
  git -C $repoDir tag base
  Add-Content (Join-Path $repoDir 'src/a.cs') '// edit'
  Set-Content (Join-Path $repoDir 'src/b.cs') 'class B {}'
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
  $toolingTree = Get-AreaTreeId $repoDir 'tooling'

  $folder = Join-Path $work "$name-folder"
  New-Item -ItemType Directory -Force $folder | Out-Null
  $step2Mark = if ($change.Step2Open) { ' ' } else { 'x' }
  $lines = @('Cycle: 1', '', '# Plan', '')
  if ($change.EarlierFrontendRun -or $change.EarlierFilesLine -or $change.EarlierDeviationFile) {
    $lines += '### Cycle 0', ''
    if ($change.EarlierFilesLine) { $lines += '- [x] Step 0: Add the extra file.', "    *   Files: ``$($change.EarlierFilesLine)``" }
    if ($change.EarlierFrontendRun) { Write-CaseResult $repoDir 'full' 'frontend' $frontendTree }
    if ($change.EarlierDeviationFile) { $lines += '', '#### Deviations', "*   Step 0: Added $($change.EarlierDeviationFile) because the helper was missing." }
    $lines += ''
  }
  $lines += '### Cycle 1', '', 'Spec: **Feature A**, AC1.', ''
  $lines += '- [x] Step 1: Change the code with a TDD red step.', '    *   Files: `src/a.cs`, `src/b.cs`, `frontend/app.ts`, `.claude/tree-id.ps1`, `.claude/tests/flow-scripts.tests.ps1`'
  if (-not $change.NoRedLine) { $lines += '    *   Red: the new test fails first with `Expected 1, found 0`.' }
  $step2Text = if ($change.Step2Tdd) { 'Document it with a TDD red step.' } elseif ($change.Step2ProofRequired) { 'Test it. Proof required.' } else { 'Document it.' }
  $step2Line = if ($change.Step2Line) { $change.Step2Line } else { "- [$step2Mark] Step 2: $step2Text" }
  $lines += $step2Line, '    *   Files: `src/a.cs`'
  if ($change.Step2Red) { $lines += '    *   Red: the doc test fails first with `Expected 2, found 0`.' }
  if ($change.Step2Proof) { $lines += '    *   Proof: the doc test fails without the guard; restored, checked with git diff' }
  if ($change.Step2Lines) { $lines += $change.Step2Lines }
  $lines += ''
  $runMode = if ($change.RunMode) { $change.RunMode } else { 'full' }
  if (-not $change.NoFullRun) { Write-CaseResult $repoDir $runMode 'backend' $backendTree }
  if ($change.ToolingRun) { Write-CaseResult $repoDir 'affected' 'tooling' $toolingTree }
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
  Assert-That ($flowCase.Exit -eq 1 -and $flowCase.Output.Count -eq 2 -and $flowCase.Output[1] -like 'Text checks failed*' -and $flowCase.Output[0] -like "Step 2: *$($case[2])*") `
    "the flow line '$($case[1].Trim())' fails with its line"
}
$retriedFail = Invoke-Handoff (New-HandoffCase 'ho-flow-retried' @{ Step2Lines = @('    *   fail: the slot did not start; retried: up') })
Assert-That ($retriedFail.Exit -eq 0 -and $retriedFail.Output.Count -eq 0) 'a fail line of a transient error with retried: passes'

$waitingTransient = Invoke-Handoff (New-HandoffCase 'ho-waiting-transient' @{
    Step2Line = '- [!] Step 2: Browser check of the page. WAITING: Chrome timed out' })
Assert-That ($waitingTransient.Exit -eq 1 -and @($waitingTransient.Output | Where-Object { $_ -like "Step 2: a transient error without 'retried:'*" }).Count -eq 1) `
  'a waiting browser check step with a transient error and no retried: fails with its line'

$waitingCheck = Invoke-Handoff (New-HandoffCase 'ho-waiting' @{
    Step2Line = '- [!] Step 2: Browser check of the page. WAITING: Docker is down; retried: restarted Docker Desktop' })
Assert-That ($waitingCheck.Exit -eq 0 -and $waitingCheck.Output.Count -eq 0) 'a waiting browser check step with retried: passes'
$waitingNoRetry = Invoke-Handoff (New-HandoffCase 'ho-waiting-noretry' @{
    Step2Line = '- [!] Step 2: Browser check of the page. WAITING: Docker is down' })
Assert-That ($waitingNoRetry.Exit -eq 1 -and $waitingNoRetry.Output[0] -eq 'Step 2 is not done ([!]).') 'a waiting browser check step without retried: fails'
$waitingOther = Invoke-Handoff (New-HandoffCase 'ho-waiting-other' @{
    Step2Line = '- [!] Step 2: Document it. WAITING: step 1; retried: none' })
Assert-That ($waitingOther.Exit -eq 1 -and $waitingOther.Output[0] -eq 'Step 2 is not done ([!]).') 'a waiting step that is no browser check fails'

$openStep = Invoke-Handoff (New-HandoffCase 'ho-open' @{ Step2Open = $true })
Assert-That ($openStep.Exit -eq 1 -and $openStep.Output.Count -eq 2 -and $openStep.Output[1] -like 'Text checks failed*' -and $openStep.Output[0] -match 'Step 2 is not done') `
  'a step that is not done fails with its line'

$noRed = Invoke-Handoff (New-HandoffCase 'ho-nored' @{ NoRedLine = $true })
Assert-That ($noRed.Exit -eq 1 -and $noRed.Output.Count -eq 2 -and $noRed.Output[1] -like 'Text checks failed*' -and $noRed.Output[0] -match 'Step 1 names TDD, Red, or Proof required') `
  'a TDD step without a Red: line fails with its line'

$secondNoRed = Invoke-Handoff (New-HandoffCase 'ho-second' @{ Step2Tdd = $true })
Assert-That ($secondNoRed.Exit -eq 1 -and $secondNoRed.Output.Count -eq 2 -and $secondNoRed.Output[1] -like 'Text checks failed*' -and
  $secondNoRed.Output[0] -match 'Step 2 names TDD, Red, or Proof required') `
  'a second TDD step without a Red: line fails, although the first step has one'

$mentionsTdd = Invoke-Handoff (New-HandoffCase 'ho-mentions-tdd' @{
    Step2Lines = @('    *   A TDD step needs `Red: <test>` and not only Proof: lines.', '    *   Proof: the guard test fails without it; restored, checked with git diff') })
Assert-That ($mentionsTdd.Exit -eq 0 -and $mentionsTdd.Output.Count -eq 0) `
  'a step without TDD on its first line that mentions TDD, Red:, and Proof: in its text passes'
$tddProofOnly = Invoke-Handoff (New-HandoffCase 'ho-tdd-proof-only' @{ Step2Tdd = $true; Step2Proof = $true })
Assert-That ($tddProofOnly.Exit -eq 1 -and $tddProofOnly.Output[0] -eq
  'Step 2 is TDD, but its block has only Proof: lines: a TDD step needs the Red: line of its red run.') `
  'a TDD step with only a Proof: line fails with its line'
$tddRedAndProof = Invoke-Handoff (New-HandoffCase 'ho-tdd-red-proof' @{ Step2Tdd = $true; Step2Red = $true; Step2Proof = $true })
Assert-That ($tddRedAndProof.Exit -eq 0 -and $tddRedAndProof.Output.Count -eq 0) 'a TDD step with a Red: line and a Proof: line passes'
$requiredProof = Invoke-Handoff (New-HandoffCase 'ho-required-proof' @{ Step2ProofRequired = $true; Step2Proof = $true })
Assert-That ($requiredProof.Exit -eq 0 -and $requiredProof.Output.Count -eq 0) 'a Proof required step with a Proof: line passes'

$bothRed = Invoke-Handoff (New-HandoffCase 'ho-both' @{ Step2Tdd = $true; Step2Red = $true })
Assert-That ($bothRed.Exit -eq 0 -and $bothRed.Output.Count -eq 0) 'two TDD steps that each have a Red: line pass'

$extra = Invoke-Handoff (New-HandoffCase 'ho-extra' @{ ExtraFile = 'src/extra.cs' })
Assert-That ($extra.Exit -eq 1 -and $extra.Output.Count -eq 2 -and $extra.Output[1] -like 'Text checks failed*' -and $extra.Output[0] -match 'src/extra.cs is in no Files') `
  'a changed file that no Files: line names fails with its line'

$logged = Invoke-Handoff (New-HandoffCase 'ho-logged' @{ ExtraFile = 'src/extra.cs'; DeviationFile = 'src/extra.cs' })
Assert-That ($logged.Exit -eq 0 -and $logged.Output.Count -eq 0) 'a changed file in a Deviations entry passes'

$globFile = Invoke-Handoff (New-HandoffCase 'ho-glob' @{ ExtraFile = 'src/extra.cs'; EarlierFilesLine = 'src/**/*.cs' })
Assert-That ($globFile.Exit -eq 0 -and $globFile.Output.Count -eq 0) 'a changed file under a Files: glob passes'

$otherGlob = Invoke-Handoff (New-HandoffCase 'ho-glob-other' @{ ExtraFile = 'src/extra.cs'; EarlierFilesLine = 'other/**/*.cs' })
Assert-That ($otherGlob.Exit -eq 1 -and $otherGlob.Output[0] -match 'src/extra.cs is in no Files') 'a changed file outside a Files: glob fails with its line'

$noRun = Invoke-Handoff (New-HandoffCase 'ho-norun' @{ NoFullRun = $true })
Assert-That ($noRun.Exit -eq 1 -and $noRun.Output.Count -eq 1 -and $noRun.Output[0] -match 'no passed full run has the current tree .*run-area.ps1 -Area backend') `
  'a missing stored backend result fails with its line'

$oldRun = New-HandoffCase 'ho-old-run' @{ NoFullRun = $true }
$oldPlan = Join-Path $oldRun.Folder 'plan.md'
$oldText = (Get-Content $oldPlan -Raw).Replace('#### Deviations', "*   Full run: tree $(Get-TreeId $oldRun.Repo); dotnet test -> 12 passed, 0 failed`n`n#### Deviations")
Set-Content $oldPlan $oldText
$oldRunResult = Invoke-Handoff $oldRun
Assert-That ($oldRunResult.Exit -eq 1 -and $oldRunResult.Output.Count -eq 1 -and $oldRunResult.Output[0] -match 'no passed full run has the current tree') `
  'a Full run line of plan.md without a stored result does not count'

$plannedCycle = Invoke-Handoff (New-HandoffCase 'ho-planned' @{PlannedCycle2 = $true })
Assert-That ($plannedCycle.Exit -eq 0 -and $plannedCycle.Output.Count -eq 0) `
  'a planned cycle 2 below the open cycle 1 does not fail the handoff of cycle 1'

$areaStale = Invoke-Handoff (New-HandoffCase 'ho-area-stale' @{StaleTree = $true })
Assert-That ($areaStale.Exit -eq 1 -and $areaStale.Output.Count -eq 1 -and $areaStale.Output[0] -match 'no passed full run has the current tree') `
  'a stale backend result (an old tree) fails with its line'

$areaFrontend = Invoke-Handoff (New-HandoffCase 'ho-area-fe' @{FrontendFile = $true })
Assert-That ($areaFrontend.Exit -eq 1 -and $areaFrontend.Output.Count -eq 1 -and
  $areaFrontend.Output[0] -match 'frontend area changed, but no passed full run has the current tree') 'a changed frontend without its stored result fails'

$areaEarlier = Invoke-Handoff (New-HandoffCase 'ho-area-earlier' @{FrontendFile = $true; EarlierFrontendRun = $true })
Assert-That ($areaEarlier.Exit -eq 0 -and $areaEarlier.Output.Count -eq 0) `
  'a stored full frontend result with the current tree passes'

$earlierFiles = Invoke-Handoff (New-HandoffCase 'ho-earlier-files' @{ExtraFile = 'src/extra.cs'; EarlierFilesLine = 'src/extra.cs' })
Assert-That ($earlierFiles.Exit -eq 0 -and $earlierFiles.Output.Count -eq 0) `
  'a changed file in a Files: line of an earlier cycle passes (the default base holds the earlier cycles)'

$earlierDeviation = Invoke-Handoff (New-HandoffCase 'ho-earlier-dev' @{ExtraFile = 'src/extra.cs'; EarlierDeviationFile = 'src/extra.cs' })
Assert-That ($earlierDeviation.Exit -eq 0 -and $earlierDeviation.Output.Count -eq 0) `
  'a changed file in the #### Deviations of an earlier cycle passes'

$lateClaude = Invoke-Handoff (New-HandoffCase 'ho-late-claude' @{LateClaudeFile = $true; ToolingRun = $true; DeviationFile = '.claude/commands/late.md' })
Assert-That ($lateClaude.Exit -eq 0 -and $lateClaude.Output.Count -eq 0) `
  'a .claude change after the backend result keeps the backend result current'

$cycleLogged = Invoke-Handoff (New-HandoffCase 'ho-cycle-dev' @{ExtraFile = 'src/extra.cs'; DeviationFile = 'src/extra.cs' })
Assert-That ($cycleLogged.Exit -eq 0 -and $cycleLogged.Output.Count -eq 0) 'a changed file in the #### Deviations of the cycle passes'

$planWide = New-HandoffCase 'ho-plan-wide' @{ ExtraFile = 'src/extra.cs' }
Add-Content (Join-Path $planWide.Folder 'plan.md') @('', '## Deviations', '', '*   Steps 1-2: Added src/extra.cs because the helper was missing.')
$planWideResult = Invoke-Handoff $planWide
Assert-That ($planWideResult.Exit -eq 1 -and $planWideResult.Output.Count -eq 2 -and $planWideResult.Output[1] -like 'Text checks failed*' -and $planWideResult.Output[0] -match 'src/extra.cs is in no Files') `
  'a changed file that only an old plan-wide ## Deviations entry names fails'

$devProof = Invoke-Handoff (New-HandoffCase 'ho-dev-proof' @{DeviationProof = $true })
Assert-That ($devProof.Exit -eq 1 -and $devProof.Output.Count -eq 2 -and $devProof.Output[1] -like 'Text checks failed*' -and $devProof.Output[0] -match 'holds a Red: or Proof: line') `
  'a Proof: line in the Deviations fails, and it does not count for the step block'

$devDone = Invoke-Handoff (New-HandoffCase 'ho-dev-done' @{DeviationDone = $true })
Assert-That ($devDone.Exit -eq 1 -and $devDone.Output.Count -eq 2 -and $devDone.Output[1] -like 'Text checks failed*' -and $devDone.Output[0] -match 'only logs a run') `
  'a "Step N: done" entry in the Deviations fails with its line'

$proofRequired = Invoke-Handoff (New-HandoffCase 'ho-proof-required' @{Step2ProofRequired = $true })
Assert-That ($proofRequired.Exit -eq 1 -and $proofRequired.Output.Count -eq 2 -and $proofRequired.Output[1] -like 'Text checks failed*' -and $proofRequired.Output[0] -match 'Step 2 names TDD, Red, or Proof required') `
  'a Proof required step without a Red: or Proof: line fails with its line'
$defaultBase = Invoke-Handoff (New-HandoffCase 'ho-default-base' @{}) @() @('-MainBranch', 'main')
Assert-That ($defaultBase.Exit -eq 0 -and $defaultBase.Output.Count -eq 0) 'without -Base, the merge base of HEAD and the main branch is the base'

# A branch that holds a commit of another feature (a main branch that moved): origin/main points at it, the local
# main is behind (Step 82).
function New-OriginCase([string] $name, [bool] $hasOrigin, [bool] $hasBaseLine = $false) {
  $case = New-HandoffCase $name
  if ($hasBaseLine) {
    $planFile = Join-Path $case.Folder 'plan.md'
    $planText = @(Get-Content $planFile)
    Set-Content $planFile (@($planText[0], "Base: $(git -C $case.Repo rev-parse base)") + $planText[1..($planText.Count - 1)])
  }
  git -C $case.Repo switch -q -c feature
  New-Item -ItemType Directory -Force (Join-Path $case.Repo '.claude/commands') | Out-Null
  Set-Content (Join-Path $case.Repo '.claude/commands/other.md') '# another feature'
  git -C $case.Repo add .claude/commands/other.md
  git -C $case.Repo commit -q -m 'another feature' -- .claude/commands/other.md
  if ($hasOrigin) { git -C $case.Repo update-ref refs/remotes/origin/main HEAD }
  return $case
}
$originBase = Invoke-Handoff (New-OriginCase 'ho-origin' $true) @() @('-MainBranch', 'main')
Assert-That ($originBase.Exit -eq 0 -and $originBase.Output.Count -eq 0 -and $originBase.Notes.Count -eq 0) `
  'with origin/main ahead of the local main, the base is the merge base with origin/main and only the files of the branch count'
$noOrigin = Invoke-Handoff (New-OriginCase 'ho-no-origin' $false) @() @('-MainBranch', 'main')
Assert-That ($noOrigin.Exit -eq 1 -and $noOrigin.Output[0] -like '*.claude/commands/other.md is in no Files*' -and
  $noOrigin.Notes.Count -eq 1 -and $noOrigin.Notes[0] -eq 'Note: origin/main not found; the base is the merge base with the local main.') `
  'without origin/main, the base is the merge base with the local main and the note is printed'
$planBaseLine = Invoke-Handoff (New-OriginCase 'ho-plan-base' $true $true) @() @('-MainBranch', 'main')
Assert-That ($planBaseLine.Exit -eq 1 -and $planBaseLine.Output[0] -like '*.claude/commands/other.md is in no Files*' -and
  $planBaseLine.Notes.Count -eq 0) 'a Base: line of the plan wins over origin/main'
$explicitBase = Invoke-Handoff (New-OriginCase 'ho-explicit-base' $true) @() @('-Base', 'base', '-MainBranch', 'main')
Assert-That ($explicitBase.Exit -eq 1 -and $explicitBase.Output[0] -like '*.claude/commands/other.md is in no Files*' -and
  $explicitBase.Notes.Count -eq 0) 'an explicit -Base wins over origin/main'

$noBase = Invoke-Handoff (New-HandoffCase 'ho-no-base' @{}) @() @('-MainBranch', 'nope')
Assert-That ($noBase.Exit -eq 1 -and ($noBase.Output -join ' ') -match 'No merge base') 'without -Base and without the main branch, the check fails with a message'

$toolingNoRun = Invoke-Handoff (New-HandoffCase 'ho-tooling-norun' @{ScriptChange = $true })
Assert-That ($toolingNoRun.Exit -eq 1 -and $toolingNoRun.Output.Count -eq 1 -and $toolingNoRun.Output[0] -match 'tooling area changed, but no passed run has the current tree .*run-tests.ps1 -Area tooling -Changed') `
  'a changed .claude/ file without a stored tooling result fails with its line, and no script test suite runs'

$toolingRun = Invoke-Handoff (New-HandoffCase 'ho-tooling-run' @{ScriptChange = $true; ToolingRun = $true })
Assert-That ($toolingRun.Exit -eq 0 -and $toolingRun.Output.Count -eq 0) 'a stored tooling result with the current tree passes'
$corruptCase = New-HandoffCase 'ho-text-first' @{ Step2Open = $true }
Set-Content (Join-Path $corruptCase.Repo '.git/claude-runs/result-backend-full.json') 'this is not json'
$textFirst = Invoke-Handoff $corruptCase
Assert-That ($textFirst.Exit -eq 1 -and $textFirst.Output.Count -eq 2 -and $textFirst.Output[0] -match 'Step 2 is not done' -and
  $textFirst.Output[1] -eq 'Text checks failed: the run results were not checked.') `
  'a missing tick exits with the Text checks failed line and reads no result file'
Assert-That ((Select-String -Path (Join-Path $ScriptsDir 'check-handoff.ps1') -Pattern 'format-changed|FormatScript').Count -eq 0) `
  'check-handoff starts no format script'

$middleOk = Invoke-Handoff (New-HandoffCase 'ho-middle-ok' @{ PlannedCycle2 = $true; RunMode = 'affected' })
Assert-That ($middleOk.Exit -eq 0 -and $middleOk.Output.Count -eq 0) 'a middle cycle passes with an affected result'
$middleNone = Invoke-Handoff (New-HandoffCase 'ho-middle-none' @{ PlannedCycle2 = $true; NoFullRun = $true })
Assert-That ($middleNone.Exit -eq 1 -and $middleNone.Output.Count -eq 1 -and
  $middleNone.Output[0] -match 'backend area changed, but no passed run has the current tree .*run-tests.ps1 -Area backend -Changed') `
  'a middle cycle without a stored result fails with the run-tests line'
$lastAffected = Invoke-Handoff (New-HandoffCase 'ho-last-affected' @{ RunMode = 'affected' })
Assert-That ($lastAffected.Exit -eq 1 -and $lastAffected.Output.Count -eq 1 -and
  $lastAffected.Output[0] -match 'backend area changed, but no passed full run has the current tree .*run-area.ps1 -Area backend') `
  'the last cycle fails with only an affected backend result'
$failedResult = New-HandoffCase 'ho-fail-result' @{ NoFullRun = $true }
Write-CaseResult $failedResult.Repo 'full' 'backend' (Get-AreaTreeId $failedResult.Repo 'backend') 'fail'
$failedResultRun = Invoke-Handoff $failedResult
Assert-That ($failedResultRun.Exit -eq 1 -and $failedResultRun.Output[0] -match 'no passed full run has the current tree') 'a failed result does not count'

}

# --- check-review-routing --------------------------------------------------------------------------------------
if (Test-Section 'check-review-routing') {
Write-Host 'check-review-routing'

$oldReview = Join-Path $work 'review-old-layout.md'
Set-Content $oldReview @(
  'Cycle: 1', 'Round: 1', '', '# Review', '', '## Open Findings', '',
  '| Severity | [CODE] | [SPEC] |', '|---|---|---|', '| BLOCKER | 0 | 0 |', '| MAJOR | 1 | 0 |', '| MINOR | 0 | 0 |', '',
  'Verdict: CHANGES_REQUESTED')
$oldLayout = Invoke-Script 'check-review-routing.ps1' @($oldReview, '-Scope', 'cycle')
Assert-That ($oldLayout.Exit -eq 1 -and ($oldLayout.Output -join ' ') -match 'review.md has no "## Open" section') `
  'a review.md without "## Open" fails with its line'

# The current layout: "## Open" with one block per open finding, "## Closed" with one line per closed finding.
function New-OpenReviewFile([string] $name, [hashtable] $change = @{}) {
  $path = Join-Path $work "$name.md"
  $heading = if ($change.BadHeading) { '### F1 — MAJOR — The save ignores the row version' } else { '### F1 MAJOR [CODE] The save ignores the row version' }
  $status = if ($change.OpenReopened) { '*   Status: REOPENED (2)' } else { '*   Status: NEW' }
  $majorCode = if ($change.WrongTable) { 2 } elseif ($change.NoOpen) { 0 } else { 1 }
  $verdict = if ($change.Approve) { 'APPROVE' } else { 'CHANGES_REQUESTED' }
  $scopeLine = if ($change.Branch) { 'Scope: branch' } else { '' }
  $lateScope = if ($change.ScopeLate) { @('Scope: branch', '') } else { @() }
  $codeBlock = if ($change.NoOpen -or $change.OnlySpec) { @() } else { @($heading, $status, '*   Where: `src/a.cs:10`', '') }
  $specBlock = if ($change.SpecFinding -or $change.OnlySpec) { @('### F3 MINOR [SPEC] The spec has a gap', '*   Status: NEW', '*   Where: `spec.md`', '') } else { @() }
  $table = if ($change.NoTable) { @() } else {
    @('| Severity | [CODE] | [SPEC] |', '|---|---|---|', '| BLOCKER | 0 | 0 |', "| MAJOR | $majorCode | 0 |", '| MINOR | 0 | 0 |', '') }
  $lines = @(
    'Cycle: 1', 'Round: 2', 'Reviewed tree: 0123456789abcdef0123456789abcdef01234567', $scopeLine, '# Review', '') + $lateScope + @(
    '## Open', '') + $codeBlock + $specBlock + @(
    '## Closed', '', '*   F2 MINOR [SPEC] An old spec gap: REOPENED (2), then ACCEPTED (Artur, 2026-11-13).', '',
    '## Rounds', '', '*   Cycle 1, round 1: tree 0123456789abcdef0123456789abcdef01234567.', '',
    '## Summary', '') + $table + @("Verdict: $verdict")
  Set-Content $path $lines
  return $path
}

$openClean = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open'), '-Scope', 'cycle')
Assert-That ($openClean.Exit -eq 0 -and ($openClean.Output -join '') -match '^executor code-fixes') `
  'an open [CODE] finding routes to the executor; a REOPENED (2) in ## Closed does not count'

$openReopened = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-reopened' @{ OpenReopened = $true }), '-Scope', 'cycle')
Assert-That ($openReopened.Exit -eq 0 -and ($openReopened.Output -join '') -match '^spec not-converging') `
  'a REOPENED (2) in ## Open routes to the spec session'

$wrongTable = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-table' @{ WrongTable = $true }), '-Scope', 'cycle')
Assert-That ($wrongTable.Exit -eq 0 -and ($wrongTable.Output -join '') -eq 'executor code-fixes (cycle 1, round 2)') `
  'a summary table that differs from ## Open is ignored'

$noTable = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-no-table' @{ NoTable = $true }), '-Scope', 'cycle')
Assert-That ($noTable.Exit -eq 0 -and ($noTable.Output -join '') -eq 'executor code-fixes (cycle 1, round 2)') `
  'a review.md without a summary table routes'

$mixed = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-mixed' @{ SpecFinding = $true }), '-Scope', 'cycle')
Assert-That ($mixed.Exit -eq 0 -and ($mixed.Output -join '') -eq 'executor code-fixes (cycle 1, round 2, open [SPEC] 1)') `
  'a round with 1 [CODE] and 1 [SPEC] finding routes to the executor and names the open [SPEC] count'

$onlySpec = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-only-spec' @{ OnlySpec = $true }), '-Scope', 'cycle')
Assert-That ($onlySpec.Exit -eq 0 -and ($onlySpec.Output -join '') -eq 'spec spec-findings (cycle 1, round 2, open [SPEC] 1)') `
  'a round with only [SPEC] findings routes to the spec session'

$mixedNotConverging = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-mixed-reopened' @{ SpecFinding = $true; OpenReopened = $true }), '-Scope', 'cycle')
Assert-That ($mixedNotConverging.Exit -eq 0 -and ($mixedNotConverging.Output -join '') -match '^spec not-converging') `
  'a not-converging round with [CODE] and [SPEC] findings routes to the spec session'

$badHeading = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-heading' @{ BadHeading = $true }), '-Scope', 'cycle')
Assert-That ($badHeading.Exit -eq 1 -and ($badHeading.Output -join ' ') -match 'is not .### F<n>') `
  'a finding heading in another format fails'

$badApprove = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-approve' @{ Approve = $true }), '-Scope', 'cycle')
Assert-That ($badApprove.Exit -eq 1 -and ($badApprove.Output -join ' ') -match 'APPROVE with 1 open') `
  'APPROVE with an open MAJOR finding fails'

$approved = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-open-routes-approve' @{ Approve = $true; NoOpen = $true }), '-Scope', 'cycle')
Assert-That ($approved.Exit -eq 0 -and ($approved.Output -join '') -match '^executor approved \(cycle 1, round 2\)') `
  'an APPROVE routes to the executor'

$branchApproved = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-branch-approve' @{ Approve = $true; NoOpen = $true; Branch = $true }), '-Scope', 'branch')
Assert-That ($branchApproved.Exit -eq 0 -and ($branchApproved.Output -join '') -eq 'branch approved (cycle 1)') `
  'a Scope: branch review with APPROVE routes branch approved'

$branchFindings = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-branch-findings' @{ Branch = $true }), '-Scope', 'branch')
Assert-That ($branchFindings.Exit -eq 0 -and ($branchFindings.Output -join '') -eq 'branch findings (cycle 1)') `
  'a Scope: branch review with CHANGES_REQUESTED routes branch findings'

$scopeLate = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-scope-late' @{ ScopeLate = $true }), '-Scope', 'cycle')
Assert-That ($scopeLate.Exit -eq 1 -and ($scopeLate.Output -join ' ') -like "*line 4 does not match the scope cycle: write or remove 'Scope: branch'.*") `
  '-Scope cycle with a Scope: branch line below line 4 fails'

$cycleLeftover = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-cycle-leftover' @{ Branch = $true }), '-Scope', 'cycle')
Assert-That ($cycleLeftover.Exit -eq 1 -and ($cycleLeftover.Output -join ' ') -like "*line 4 does not match the scope cycle: write or remove 'Scope: branch'.*") `
  '-Scope cycle with a leftover Scope: branch line fails'

$branchMissing = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-branch-missing'), '-Scope', 'branch')
Assert-That ($branchMissing.Exit -eq 1 -and ($branchMissing.Output -join ' ') -like "*line 4 does not match the scope branch: write or remove 'Scope: branch'.*") `
  '-Scope branch without the Scope: branch line fails'

$branchLate = Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-branch-late' @{ ScopeLate = $true }), '-Scope', 'branch')
Assert-That ($branchLate.Exit -eq 1 -and ($branchLate.Output -join ' ') -like "*line 4 does not match the scope branch*") `
  '-Scope branch with the line below line 4 fails'

$branchBadApprove =Invoke-Script 'check-review-routing.ps1' @((New-OpenReviewFile 'review-branch-bad-approve' @{ Approve = $true; Branch = $true }), '-Scope', 'branch')
Assert-That ($branchBadApprove.Exit -eq 1 -and ($branchBadApprove.Output -join ' ') -match 'APPROVE with 1 open') `
  'a Scope: branch review with APPROVE and an open MAJOR still fails'

}

# --- commit-cycle ----------------------------------------------------------------------------------------------
if (Test-Section 'commit-cycle') {
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
  $waitingLines = if ($change.WaitingBrowser) { @('- [!] Step 0: Browser check of the page. WAITING: Docker is down; retried: restarted Docker Desktop') }
  elseif ($change.BlockedBrowser) { @('- [!] Step 0: Browser check of the page. BLOCKED: flow 2 failed; proposal: fix the drawer') } else { @() }
  $nextCycleLines = if ($change.NextCycle) { @('### Cycle 2', '', 'Spec: **Feature A**, AC1.', '', '- [ ] Step 5: Plan the next thing.') } else { @() }
  $codeSubject = if ($change.BadSubject) { 'Feature(sample): Changed a' } else { 'feature(sample): changed a and b' }
  $openCycle = if ($change.EarlierWaiting) { 2 } else { 1 }
  $earlierLines = if ($change.EarlierWaiting) {
    @('### Cycle 1', '', 'Spec: **Feature A**, AC1.', '',
      '- [!] Step 0: Browser check of the page. WAITING: Docker is down; retried: restarted Docker Desktop', '', '#### Deviations', '')
  } else { @() }
  Set-Content (Join-Path $folder 'plan.md') (@("Cycle: $openCycle", '', '# Plan', '') + $earlierLines + @("### Cycle $openCycle", '', 'Spec: **Feature A**, AC1.',
      'Commit tooling: feature(setup): changed x') + $docsLine + @("Commit code: $codeSubject", '',
      '- [x] Step 1: Change the code.', '    *   Files: `src/a.cs`') + $waitingLines + @('', '#### Deviations', '') + $nextCycleLines)
  $verdict = if ($change.ChangesRequested) { 'CHANGES_REQUESTED' } else { 'APPROVE' }
  $reviewCycle = if ($change.ReviewCycle) { $change.ReviewCycle } else { $openCycle }
  Set-Content (Join-Path $folder 'review.md') @("Cycle: $reviewCycle", 'Round: 1', "Reviewed tree: $(Get-TreeId $repoDir)", '',
    '## Open', '', '## Closed', '', '## Summary', '', '| Severity | [CODE] | [SPEC] |', '|---|---|---|',
    '| BLOCKER | 0 | 0 |', '| MAJOR | 0 | 0 |', '| MINOR | 0 | 0 |', '', "Verdict: $verdict")
  if ($change.LateChange) { Add-Content (Join-Path $repoDir 'src/a.cs') '// late' }
  return @{ Folder = $folder; Repo = $repoDir; Remote = $remote; Base = $base }
}

function Invoke-Commit($case) {
  return Invoke-Script 'commit-cycle.ps1' @($case.Folder, '-Repo', $case.Repo)
}

$waitingLast = New-CommitCase 'cc-waiting-last' @{ WaitingBrowser = $true }
$waitingLastHead = git -C $waitingLast.Repo rev-parse HEAD
$waitingLastRun = Invoke-Commit $waitingLast
Assert-That ($waitingLastRun.Exit -eq 1 -and
  ($waitingLastRun.Output -contains 'Step 0 is a waiting browser check: the last cycle commits after its flows pass.') -and
  (git -C $waitingLast.Repo rev-parse HEAD) -eq $waitingLastHead) 'the last cycle with a waiting browser check step makes no commit'
$waitingEarlier = New-CommitCase 'cc-waiting-earlier' @{ EarlierWaiting = $true }
$waitingEarlierHead = git -C $waitingEarlier.Repo rev-parse HEAD
$waitingEarlierRun = Invoke-Commit $waitingEarlier
Assert-That ($waitingEarlierRun.Exit -eq 1 -and
  ($waitingEarlierRun.Output -contains 'Step 0 is a waiting browser check: the last cycle commits after its flows pass.') -and
  (git -C $waitingEarlier.Repo rev-parse HEAD) -eq $waitingEarlierHead) 'the last cycle with a waiting browser check step of an earlier cycle makes no commit'
$blockedLast = New-CommitCase 'cc-blocked-last' @{ BlockedBrowser = $true }
$blockedLastHead = git -C $blockedLast.Repo rev-parse HEAD
$blockedLastRun = Invoke-Commit $blockedLast
Assert-That ($blockedLastRun.Exit -eq 1 -and
  ($blockedLastRun.Output -contains 'Step 0 is a blocked browser check: the last cycle commits after its flows pass.') -and
  (git -C $blockedLast.Repo rev-parse HEAD) -eq $blockedLastHead) 'the last cycle with a blocked browser check step makes no commit'
$blockedMiddle = New-CommitCase 'cc-blocked-middle' @{ BlockedBrowser = $true; NextCycle = $true }
$blockedMiddleRun = Invoke-Commit $blockedMiddle
Assert-That ($blockedMiddleRun.Exit -eq 0 -and @(git -C $blockedMiddle.Repo log --format=%s "$($blockedMiddle.Base)..HEAD").Count -eq 3) `
  'a middle cycle with a blocked browser check step commits'
$waitingMiddle = New-CommitCase 'cc-waiting-middle' @{ WaitingBrowser = $true; NextCycle = $true }
$waitingMiddleRun = Invoke-Commit $waitingMiddle
Assert-That ($waitingMiddleRun.Exit -eq 0 -and @(git -C $waitingMiddle.Repo log --format=%s "$($waitingMiddle.Base)..HEAD").Count -eq 3) `
  'a middle cycle with a waiting browser check step commits'

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

# Fixups: a repo with the base on main, develop at the base, and 2 feature commits ("added a" creates src/a.cs, "added b"
# edits its line two). The work tree then edits src/a.cs (the fixup entry) and adds src/c.cs (the code group).
function New-FixupCase([string] $name, [hashtable] $change = @{}) {
  $repoDir = New-Repo "$name-repo"
  New-Item -ItemType Directory -Force (Join-Path $repoDir 'src') | Out-Null
  Set-Content (Join-Path $repoDir 'base.txt') 'base'
  Save-Commit $repoDir 'base'
  git -C $repoDir branch develop
  $base = git -C $repoDir rev-parse HEAD
  Set-Content (Join-Path $repoDir 'src/a.cs') @('one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight')
  Save-Commit $repoDir 'feature(sample): added a'
  Set-Content (Join-Path $repoDir 'src/a.cs') @('one', 'TWO', 'three', 'four', 'five', 'six', 'seven', 'eight')
  Save-Commit $repoDir 'feature(sample): added b'
  $reviewed = git -C $repoDir rev-parse HEAD
  $fixedLines = if ($change.Conflict) { @('one', 'deux', 'three', 'four', 'five', 'six', 'seven', 'eight') } else {
    @('one', 'TWO', 'three', 'four', 'five', 'six', 'seven', 'EIGHT') }
  Set-Content (Join-Path $repoDir 'src/a.cs') $fixedLines
  if (-not $change.FixupOnly) { Set-Content (Join-Path $repoDir 'src/c.cs') 'class C {}' }
  if ($change.TwoSteps) { Set-Content (Join-Path $repoDir 'src/e.cs') 'class E {}' }

  $folder = Join-Path $work "$name-folder"
  New-Item -ItemType Directory -Force $folder | Out-Null
  $subject = if ($change.Subject) { $change.Subject } else { 'feature(sample): added a' }
  $header = @('Cycle: 1')
  if ($change.Reviewed) { $header += "Reviewed by Artur: $reviewed" }
  if ($change.BaseLine) { $header += "Base: $base" }
  if ($change.StaleMark) { $header += 'Reviewed by Artur: 0123456789012345678901234567890123456789' }
  $commitLines = if ($change.NoCommitLine) { @() } elseif ($change.CommitLine) { @($change.CommitLine) } else { @('Commit code: feature(sample): added c') }
  $plainFiles = if ($change.CancelledFixup -or $change.PlainShares) { '`src/a.cs`, `src/c.cs`' } else { '`src/c.cs`' }
  $plainStep = if ($change.FixupOnly) { @() } else { @('- [x] Step 2: Add c.', "    *   Files: $plainFiles", '') }
  if ($change.BrowserStep) { $plainStep += @('- [x] Step 4: Browser check.', '    *   Files: none', '') }
  $step1Mark = if ($change.CancelledFixup) { '~' } else { 'x' }
  $step3Subject = if ($change.Step3Subject) { $change.Step3Subject } else { $subject }
  $step3Files = if ($change.Step3Files) { $change.Step3Files } else { '`src/e.cs`' }
  $step3 = if ($change.TwoSteps) { @('- [x] Step 3: Fix a again.', "    *   Files: $step3Files", "    *   Fixup: $step3Subject", '') } else { @() }
  $deviationLines = if ($change.DeviationLines) { $change.DeviationLines } else { @() }
  Set-Content (Join-Path $folder 'plan.md') ($header + @('', '# Plan', '', '### Cycle 1', '', 'Spec: **Feature A**, AC1.') +
    $commitLines + @('', "- [$step1Mark] Step 1: Fix a.", '    *   Files: `src/a.cs`', "    *   Fixup: $subject", '') + $plainStep + $step3 +
    @('#### Deviations', '') + $deviationLines)
  Set-Content (Join-Path $folder 'review.md') @('Cycle: 1', 'Round: 1', "Reviewed tree: $(Get-TreeId $repoDir)", '',
    '## Open', '', '## Closed', '', '## Summary', '', '| Severity | [CODE] | [SPEC] |', '|---|---|---|',
    '| BLOCKER | 0 | 0 |', '| MAJOR | 0 | 0 |', '| MINOR | 0 | 0 |', '', 'Verdict: APPROVE')
  return @{ Folder = $folder; Repo = $repoDir; Base = $base; Reviewed = $reviewed }
}

function Get-LogSubjects($case) { return @(git -C $case.Repo log --format=%s "$($case.Base)..HEAD") }

$foldCase = New-FixupCase 'cc-fold'
$workTree = (git -C $foldCase.Repo show 'HEAD:src/a.cs') -join '|'
$treeBefore = Get-TreeId $foldCase.Repo
$folded = Invoke-Commit $foldCase
$foldSubjects = Get-LogSubjects $foldCase
Assert-That ($folded.Exit -eq 0 -and $foldSubjects.Count -eq 3 -and $foldSubjects[0] -eq 'feature(sample): added c' -and
  $foldSubjects[1] -eq 'feature(sample): added b' -and $foldSubjects[2] -eq 'feature(sample): added a' -and
  -not ($foldSubjects -match '^fixup!')) 'a fixup commit folds into its target: 3 commits, no fixup! commit'
Assert-That ((Get-TreeId $foldCase.Repo) -eq $treeBefore -and (git -C $foldCase.Repo status --porcelain).Count -eq 0 -and
  (git -C $foldCase.Repo show 'HEAD:src/a.cs') -join '|' -eq 'one|TWO|three|four|five|six|seven|EIGHT') 'the tree stays the same after the autosquash'

$reviewedCase = New-FixupCase 'cc-reviewed' @{ Reviewed = $true }
$kept = Invoke-Commit $reviewedCase
$keptSubjects = Get-LogSubjects $reviewedCase
Assert-That ($kept.Exit -eq 0 -and $keptSubjects.Count -eq 4 -and $keptSubjects[1] -eq 'fixup! feature(sample): added a') `
  'a fixup whose target is at or below the reviewed SHA stays a separate commit'

$fixupMissing = New-FixupCase 'cc-fixup-missing' @{ Subject = 'feature(sample): added z' }
$missingHead = git -C $fixupMissing.Repo rev-parse HEAD
$missing = Invoke-Commit $fixupMissing
Assert-That ($missing.Exit -eq 1 -and ($missing.Output -join ' ') -like "*the Fixup subject 'feature(sample): added z' matches no commit*" -and
  (git -C $fixupMissing.Repo rev-parse HEAD) -eq $missingHead) 'a Fixup subject with no matching commit makes no commit'

$fixupTwo = New-FixupCase 'cc-fixup-two'
git -C $fixupTwo.Repo commit -q --allow-empty -m 'feature(sample): added a'
$twoHead = git -C $fixupTwo.Repo rev-parse HEAD
$two = Invoke-Commit $fixupTwo
Assert-That ($two.Exit -eq 1 -and ($two.Output -join ' ') -like "*matches 2 commits of*" -and
  (git -C $fixupTwo.Repo rev-parse HEAD) -eq $twoHead) 'a Fixup subject with 2 matching commits makes no commit'

$conflictCase = New-FixupCase 'cc-conflict' @{ Conflict = $true }
$conflictTree = Get-TreeId $conflictCase.Repo
$conflicted = Invoke-Commit $conflictCase
$gitDir = (git -C $conflictCase.Repo rev-parse --absolute-git-dir)
Assert-That ($conflicted.Exit -eq 1 -and ($conflicted.Output -contains 'autosquash conflict: src/a.cs') -and
  ($conflicted.Output -join ' ') -like '*resolve, git add, git -c core.editor=true rebase --continue, then run commit-cycle.ps1 -Verify*' -and
  (Test-Path (Join-Path $gitDir 'rebase-merge'))) 'an autosquash conflict names the file and leaves the rebase in progress'

$env:GIT_EDITOR = 'true'
for ($round = 0; $round -lt 5 -and (Test-Path (Join-Path $gitDir 'rebase-merge')); $round++) {
  Set-Content (Join-Path $conflictCase.Repo 'src/a.cs') @('one', 'deux', 'three', 'four', 'five', 'six', 'seven', 'eight')
  git -C $conflictCase.Repo add -A
  git -C $conflictCase.Repo rebase --continue 2>&1 | Out-Null
}
Remove-Item Env:GIT_EDITOR
$verified = Invoke-Script 'commit-cycle.ps1' @($conflictCase.Folder, '-Repo', $conflictCase.Repo, '-Verify')
Assert-That ($verified.Exit -eq 0 -and (Get-TreeId $conflictCase.Repo) -eq $conflictTree) '-Verify passes after a manual rebase --continue'

$treeChanged = New-FixupCase 'cc-tree'
[void](Invoke-Commit $treeChanged)
Add-Content (Join-Path $treeChanged.Repo 'src/a.cs') '// edited after the autosquash'
Save-Commit $treeChanged.Repo 'feature(sample): edited a'
$changedVerify = Invoke-Script 'commit-cycle.ps1' @($treeChanged.Folder, '-Repo', $treeChanged.Repo, '-Verify')
Assert-That ($changedVerify.Exit -eq 1 -and ($changedVerify.Output -join ' ') -like '*the HEAD tree changed in the autosquash.*') `
  '-Verify fails when the HEAD tree differs from the stored tree'

$squashCase = New-FixupCase 'cc-squash' @{ Reviewed = $true }
[void](Invoke-Commit $squashCase)
$squashed = Invoke-Script 'commit-cycle.ps1' @($squashCase.Folder, '-Repo', $squashCase.Repo, '-Squash')
$squashSubjects = Get-LogSubjects $squashCase
Assert-That ($squashed.Exit -eq 0 -and $squashSubjects.Count -eq 3 -and -not ($squashSubjects -match '^fixup!') -and
  (git -C $squashCase.Repo show 'HEAD:src/a.cs') -join '|' -eq 'one|TWO|three|four|five|six|seven|EIGHT') `
  '-Squash folds a fixup below the reviewed SHA and keeps the tree'

$twoStepsCase = New-FixupCase 'cc-two-steps' @{ TwoSteps = $true }
$twoSteps = Invoke-Commit $twoStepsCase
$twoStepsSubjects = Get-LogSubjects $twoStepsCase
Assert-That ($twoSteps.Exit -eq 0 -and $twoStepsSubjects.Count -eq 3 -and (git -C $twoStepsCase.Repo status --porcelain).Count -eq 0 -and
  (git -C $twoStepsCase.Repo show 'HEAD:src/a.cs') -join '|' -eq 'one|TWO|three|four|five|six|seven|EIGHT' -and
  (git -C $twoStepsCase.Repo show 'HEAD:src/e.cs') -eq 'class E {}') `
  'two Fixup steps with the same subject and different files each get a fixup commit, and the checkout is clean'

Assert-That ($folded.Output.Count -eq 1 -and $folded.Output[0] -eq "$(git -C $foldCase.Repo rev-parse --short HEAD) feature(sample): added c") `
  'the printed commit lines hold the SHAs of the history after the autosquash'

$noTreeCase = New-FixupCase 'cc-no-tree'
$noTree = Invoke-Script 'commit-cycle.ps1' @($noTreeCase.Folder, '-Repo', $noTreeCase.Repo, '-Verify')
Assert-That ($noTree.Exit -eq 1 -and ($noTree.Output -join ' ') -like '*no stored autosquash tree.*') '-Verify without a stored tree reports it'

$dirtySquash = Invoke-Script 'commit-cycle.ps1' @($noTreeCase.Folder, '-Repo', $noTreeCase.Repo, '-Squash')
Assert-That ($dirtySquash.Exit -eq 1 -and ($dirtySquash.Output -join ' ') -like '*the checkout is not clean.*') '-Squash on a dirty checkout stops'

$strandedCase = New-FixupCase 'cc-stranded'
$targetSha = git -C $strandedCase.Repo log --format=%H --grep 'added a' -1
git -C $strandedCase.Repo add -A
git -C $strandedCase.Repo commit -q --fixup=$targetSha
$strandedGitDir = git -C $strandedCase.Repo rev-parse --absolute-git-dir
[IO.File]::WriteAllText((Join-Path $strandedGitDir 'claude-autosquash-tree.txt'),
  "$(git -C $strandedCase.Repo rev-parse 'HEAD^{tree}')`n$($strandedCase.Base)`n")
$stranded = Invoke-Script 'commit-cycle.ps1' @($strandedCase.Folder, '-Repo', $strandedCase.Repo, '-Verify')
Assert-That ($stranded.Exit -eq 1 -and ($stranded.Output -join ' ') -like '*a fixup! commit stays above *: * fixup! feature(sample): added a*') `
  '-Verify fails when a fixup! commit with its target above the squash base stays'

# A cancelled Fixup step is no fixup entry (Step 68).
$cancelledCase = New-FixupCase 'cc-cancelled' @{ CancelledFixup = $true }
$cancelled = Invoke-Commit $cancelledCase
$cancelledSubjects = Get-LogSubjects $cancelledCase
$cancelledFiles = @(git -C $cancelledCase.Repo show --name-only --format= HEAD) -join ' '
Assert-That ($cancelled.Exit -eq 0 -and $cancelledSubjects.Count -eq 3 -and $cancelledSubjects[0] -eq 'feature(sample): added c' -and
  $cancelledFiles -eq 'src/a.cs src/c.cs') 'a cancelled Fixup step and a plain step with the same file: the file goes in the group commit'

# A squash base that is no ancestor of HEAD (Step 66).
$staleCase = New-FixupCase 'cc-stale-mark' @{ StaleMark = $true }
$staleHead = git -C $staleCase.Repo rev-parse HEAD
$stale = Invoke-Commit $staleCase
Assert-That ($stale.Exit -eq 1 -and ($stale.Output -contains 'the squash base 0123456 is no ancestor of HEAD.') -and
  (git -C $staleCase.Repo rev-parse HEAD) -eq $staleHead) 'a Reviewed by Artur SHA that is no ancestor of HEAD makes no commit'
$stalePlan = Join-Path $staleCase.Folder 'plan.md'
Set-Content $stalePlan (Get-Content $stalePlan | Where-Object { $_ -notmatch '^Reviewed by Artur:' })
$afterRemoval = Invoke-Commit $staleCase
Assert-That ($afterRemoval.Exit -eq 0 -and (Get-LogSubjects $staleCase).Count -eq 3) 'after the mark line is removed, the same plan commits'

$staleVerifyCase = New-FixupCase 'cc-stale-verify'
$staleVerifyGitDir = git -C $staleVerifyCase.Repo rev-parse --absolute-git-dir
[IO.File]::WriteAllText((Join-Path $staleVerifyGitDir 'claude-autosquash-tree.txt'),
  "$(git -C $staleVerifyCase.Repo rev-parse 'HEAD^{tree}')`n0123456789012345678901234567890123456789`n")
$staleVerify = Invoke-Script 'commit-cycle.ps1' @($staleVerifyCase.Folder, '-Repo', $staleVerifyCase.Repo, '-Verify')
Assert-That ($staleVerify.Exit -eq 1 -and ($staleVerify.Output -contains 'the squash base 0123456 is no ancestor of HEAD.')) `
  '-Verify fails when the stored squash base is no ancestor of HEAD'

# A cycle of only fixups needs no Commit line (Step 62).
$fixupOnlyCase = New-FixupCase 'cc-fixup-only' @{ FixupOnly = $true; NoCommitLine = $true }
$fixupOnlyRun = Invoke-Commit $fixupOnlyCase
$fixupOnlySubjects = Get-LogSubjects $fixupOnlyCase
Assert-That ($fixupOnlyRun.Exit -eq 0 -and $fixupOnlySubjects.Count -eq 2 -and -not ($fixupOnlySubjects -match '^fixup!') -and
  (git -C $fixupOnlyCase.Repo status --porcelain).Count -eq 0 -and
  (git -C $fixupOnlyCase.Repo show 'HEAD:src/a.cs') -join '|' -eq 'one|TWO|three|four|five|six|seven|EIGHT') `
  'a fixup-only cycle without a Commit line makes only the fixup commit and folds it'

$noCommitLineCase = New-FixupCase 'cc-no-commit-line' @{ NoCommitLine = $true }
$noCommitHead = git -C $noCommitLineCase.Repo rev-parse HEAD
$noCommitLine = Invoke-Commit $noCommitLineCase
Assert-That ($noCommitLine.Exit -eq 1 -and ($noCommitLine.Output -join ' ') -like '*src/c.cs needs a Commit code line.*' -and
  (git -C $noCommitLineCase.Repo rev-parse HEAD) -eq $noCommitHead) `
  'a cycle with a fixup step and a plain step without a Commit line makes no commit'

# A path claimed by two fixup entries (Step 64).
$twoSubjectsCase = New-FixupCase 'cc-two-subjects' @{ TwoSteps = $true; Step3Subject = 'feature(sample): added b'; Step3Files = '`src/a.cs`' }
$twoSubjectsHead = git -C $twoSubjectsCase.Repo rev-parse HEAD
$twoSubjects = Invoke-Commit $twoSubjectsCase
Assert-That ($twoSubjects.Exit -eq 1 -and
  ($twoSubjects.Output -contains "Cycle 1: src/a.cs is in the Fixup entries of 'feature(sample): added a' and 'feature(sample): added b'.") -and
  (git -C $twoSubjectsCase.Repo rev-parse HEAD) -eq $twoSubjectsHead) `
  'a path in two fixup entries with different subjects makes no commit'

$sameSubjectCase = New-FixupCase 'cc-same-subject' @{ TwoSteps = $true; Step3Files = '`src/a.cs`' }
$sameSubject = Invoke-Commit $sameSubjectCase
Assert-That ($sameSubject.Exit -eq 0) 'two fixup entries with the same subject and the same path pass'

$inlinePlainCase = New-FixupCase 'cc-inline-plain' @{
  DeviationLines = @('*   Inline fix 1: changed c. Fixup: feature(sample): added a; files: `src/c.cs`') }
$inlinePlainHead = git -C $inlinePlainCase.Repo rev-parse HEAD
$inlinePlain = Invoke-Commit $inlinePlainCase
Assert-That ($inlinePlain.Exit -eq 1 -and
  ($inlinePlain.Output -contains 'Cycle 1: src/c.cs is in an inline fix with Fixup and in Step 2 without Fixup.') -and
  (git -C $inlinePlainCase.Repo rev-parse HEAD) -eq $inlinePlainHead) `
  'a path in an inline fix with Fixup and in a plain step makes no commit'

$browserCase = New-FixupCase 'cc-browser-step' @{ FixupOnly = $true; NoCommitLine = $true; BrowserStep = $true }
$browser = Invoke-Commit $browserCase
$browserSubjects = Get-LogSubjects $browserCase
Assert-That ($browser.Exit -eq 0 -and $browserSubjects.Count -eq 2 -and $browserSubjects[0] -eq 'feature(sample): added b') `
  'a Fixup step and a step with Files: none commit as a fixup only'

$basedCase = New-FixupCase 'cc-plan-base' @{ FixupOnly = $true; BaseLine = $true; CommitLine = 'Commit code: feature(sample): added a' }
git -C $basedCase.Repo branch -f develop (git -C $basedCase.Repo rev-parse 'HEAD~1')
$based = Invoke-Commit $basedCase
Assert-That ($based.Exit -eq 0) 'a Commit line without changed files is done by a commit since the Base: line of the plan'

$stepPlainCase = New-FixupCase 'cc-step-plain' @{ PlainShares = $true }
$stepPlainHead = git -C $stepPlainCase.Repo rev-parse HEAD
$stepPlain = Invoke-Commit $stepPlainCase
Assert-That ($stepPlain.Exit -eq 1 -and
  ($stepPlain.Output -contains 'Cycle 1: Step 1: src/a.cs is in a Fixup step and in Step 2 without Fixup.') -and
  (git -C $stepPlainCase.Repo rev-parse HEAD) -eq $stepPlainHead) `
  'a path in a fixup step and in a plain step makes no commit'

$inlineStepCase = New-FixupCase 'cc-inline-step' @{
  DeviationLines = @('*   Inline fix 1: changed a. Fixup: feature(sample): added b; files: `src/a.cs`') }
$inlineStep = Invoke-Commit $inlineStepCase
Assert-That ($inlineStep.Exit -eq 1 -and
  ($inlineStep.Output -contains "Cycle 1: src/a.cs is in the Fixup entries of 'feature(sample): added a' and 'feature(sample): added b'.")) `
  'a path in a fixup step and in an inline fix with another subject makes no commit'

}

# --- fake tools for format-changed and run-area ------------------------------------------------------------------
if (Test-Section 'format-changed', 'run-area', 'run-tests' -Quiet) {
# Each fake logs "<tool> <arguments> @ <folder name>" to FAKE_LOG. The fake npx prettier appends "// PRETTIER" to each
# file it gets. The fake dotnet answers like the Microsoft Testing Platform summary; FAKE_UNIT_FAIL makes the unit
# command fail, and FAKE_TOUCH names a file that the integration command changes.
$fakeBin = Join-Path $work 'bin'
New-Item -ItemType Directory -Force $fakeBin | Out-Null
$fakeLogLine = 'if ($env:FAKE_LOG) { Add-Content $env:FAKE_LOG "$tool $($args -join '' '') @ $(Split-Path -Leaf (Get-Location))" }'
Set-Content (Join-Path $fakeBin 'fake-npx.ps1') @(
  '$tool = ''npx''', $fakeLogLine, 'if ($env:FAKE_ARGS_LOG -and $env:HF_TEST_TAGS) { Add-Content $env:FAKE_ARGS_LOG "tags $env:HF_TEST_TAGS" }',
  'if ($args[0] -eq ''prettier'') { foreach ($file in @($args | Where-Object { $_ -notlike ''-*'' -and $_ -ne ''prettier'' })) { if (Test-Path -LiteralPath $file) { Add-Content -LiteralPath $file ''// PRETTIER'' } }; exit 0 }',
  'if ($args[1] -eq ''test'') { Write-Output ''      Tests  3 passed (3)'' }',
  'exit 0')
Set-Content (Join-Path $fakeBin 'fake-npm.ps1') @(
  '$tool = ''npm''', $fakeLogLine,
  'if ($env:FAKE_NPM_FAIL -and ($args -join '' '') -eq $env:FAKE_NPM_FAIL) { Write-Output ''lint error''; exit 1 }',
  'if ($args[0] -eq ''test'') { Write-Output '' Tests  2 passed (2)'' }',
  'exit 0')
Set-Content (Join-Path $fakeBin 'fake-dotnet.ps1') @(
  '$tool = ''dotnet''',
  '$line = $args -join '' ''',
  'if ($env:FAKE_ARGS_LOG) { Add-Content $env:FAKE_ARGS_LOG "dotnet $line" }',
  '$kind = if ($args[0] -eq ''build'') { ''build'' } elseif ($line -match ''UnitTests'') { ''unit'' } else { ''integration'' }',
  'if ($env:FAKE_LOG) { Add-Content $env:FAKE_LOG "dotnet $kind" }',
  'if ($kind -eq ''build'' -and $env:FAKE_BUILD_FAIL) { $folder = Join-Path $env:FAKE_BUILD_FAIL ''backend/App/obj/Debug''; New-Item -ItemType Directory -Force $folder | Out-Null; $uri = ''file:///'' + $env:FAKE_BUILD_FAIL.Replace(''\'', ''/'') + ''/backend/App/A.cs''; @{ version = ''2.1.0''; runs = @(@{ results = @(@{ ruleId = ''CS0103''; level = ''error''; message = @{ text = ''The name x does not exist'' }; locations = @(@{ physicalLocation = @{ artifactLocation = @{ uri = $uri }; region = @{ startLine = 7; startColumn = 9 } } }) }) }) } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $folder ''App.sarif''); exit 1 }',
  'if ($kind -eq ''build'' -and $env:FAKE_BUILD_WARN) { $folder = Join-Path $env:FAKE_BUILD_WARN ''backend/App/obj/Debug''; New-Item -ItemType Directory -Force $folder | Out-Null; $uri = ''file:///'' + $env:FAKE_BUILD_WARN.Replace(''\'', ''/'') + ''/backend/App/A.cs''; @{ version = ''2.1.0''; runs = @(@{ results = @(@{ ruleId = ''CS0219''; level = ''warning''; message = @{ text = ''The variable is assigned but never used'' }; locations = @(@{ physicalLocation = @{ artifactLocation = @{ uri = $uri }; region = @{ startLine = 3; startColumn = 5 } } }) }) }) } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $folder ''App.sarif'') }',
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

# A repository for the tool suites: the fake suites log "suite <name>" to FAKE_LOG; FAKE_SUITE_FAIL names a suite that fails.
function New-ToolingRepo([string] $name, [string[]] $changed) {
  $dir = New-Repo $name
  foreach ($folder in '.claude/rules', '.claude/agents', '.claude/commands', '.claude/hooks/tests', '.claude/tests', 'scripts/tests', 'src') {
    New-Item -ItemType Directory -Force (Join-Path $dir $folder) | Out-Null
  }
  $suiteTemplate = @('param()', 'Add-Content $env:FAKE_LOG "suite @SUITE@"', 'if ($env:FAKE_SUITE_FAIL -eq "@SUITE@") { Write-Host "  FAIL a sample check"; exit 1 }', 'Write-Host "All checks passed."')
  foreach ($suite in '.claude/tests/rules.tests.ps1', '.claude/hooks/tests/hook-scope.tests.ps1', '.claude/tests/flow-scripts.tests.ps1',
    'scripts/tests/verify-slots.tests.ps1', 'scripts/tests/agent-chrome.tests.ps1') {
    Set-Content (Join-Path $dir $suite) ($suiteTemplate | ForEach-Object { $_.Replace('@SUITE@', (Split-Path $suite -Leaf)) })
  }
  foreach ($file in $changed + @('src/a.cs')) { Set-Content (Join-Path $dir $file) 'x' }
  Save-Commit $dir 'base'
  git -C $dir tag base
  foreach ($file in $changed) { Add-Content (Join-Path $dir $file) '# edit' }
  return $dir
}

function Get-MarkCount([string] $path, [string] $mark) {
  ([regex]::Matches((Get-Content $path -Raw), [regex]::Escape($mark))).Count
}

}

# --- format-changed --------------------------------------------------------------------------------------------
if (Test-Section 'format-changed') {
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
Assert-That ((Get-MarkCount $fileA '// PRETTIER') -eq 2) 'a second call at the same tree formats the file again'
[void](Invoke-Script 'format-changed.ps1' @('-Repo', $formatRepo, '-Files', 'frontend/src/a.ts,frontend/src/b.ts'))
Assert-That ((Get-MarkCount $fileA '// PRETTIER') -eq 3 -and (Get-MarkCount $fileB '// PRETTIER') -eq 1) `
  'a call with two files formats both'
Assert-That (-not (Test-Path (Join-Path $formatRepo '.git/claude-format-state.json'))) 'format-changed writes no claude-format-state.json'

# The cleanup of C# files: a fake cleanup script in a copy of the scripts counts its calls.
$formatScripts = Join-Path $work 'format-scripts'
New-Item -ItemType Directory -Force (Join-Path $formatScripts 'hooks') | Out-Null
Copy-Item (Join-Path $ScriptsDir 'format-changed.ps1') $formatScripts
foreach ($helper in 'checkout-helpers.ps1', 'command-line-batches.ps1') { Copy-Item (Join-Path $ScriptsDir "hooks/$helper") (Join-Path $formatScripts 'hooks') }
$cleanupCalls = Join-Path $work 'cleanup-calls.txt'
Set-Content (Join-Path $formatScripts 'hooks/cleanup-changed.ps1') @('param([string[]]$Files, [string]$Repo)', "Add-Content '$cleanupCalls' ""cleanup `$(`$Files -join ',')""")
$csRepo = New-Repo 'format-cs'
Set-Content (Join-Path $csRepo 'a.cs') 'class A {}'
Save-Commit $csRepo 'base'
Add-Content (Join-Path $csRepo 'a.cs') '// edited'
foreach ($call in 1..2) { & pwsh -NoProfile -File (Join-Path $formatScripts 'format-changed.ps1') -Repo $csRepo -Files 'a.cs' | Out-Null }
Assert-That ((@(Get-Content $cleanupCalls)).Count -eq 2) 'a second call at the same tree runs the cleanup again'

}

# --- run-area --------------------------------------------------------------------------------------------------
if (Test-Section 'run-area') {
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
  Save-Commit $dir 'base'
  git -C $dir tag base
  Add-Content (Join-Path $dir 'src/a.cs') '// edit'
  Set-Content (Join-Path $dir 'src/b.cs') 'class B {}'
  if ($change.NamedIndex) { Add-Content (Join-Path $dir 'src/b.cs') 'var a = list[index: 0];' }
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

$buildRepo = New-AreaRepo 'ra-build-fail'
$buildFail = Invoke-Area $buildRepo 'backend' @{ FAKE_BUILD_FAIL = $buildRepo }
Assert-That ($buildFail.Exit -eq 1 -and $buildFail.Last -eq 'RUN FAILED backend: build failed' -and
  ($buildFail.Output -contains 'backend/App/A.cs(7,9): error CS0103: The name x does not exist') -and
  ($buildFail.Log -notcontains 'dotnet unit')) 'a failing build prints its SARIF errors as typed lines before the RUN FAILED line'

$staleRepo = New-AreaRepo 'ra-build-stale'
$staleFolder = Join-Path $staleRepo 'backend/Old/obj/Debug'
New-Item -ItemType Directory -Force $staleFolder | Out-Null
$staleUri = 'file:///' + $staleRepo.Replace('\', '/') + '/backend/Old/B.cs'
@{ version = '2.1.0'; runs = @(@{ results = @(@{ ruleId = 'CS0246'; level = 'error'; message = @{ text = 'An old error' }
          locations = @(@{ physicalLocation = @{ artifactLocation = @{ uri = $staleUri }; region = @{ startLine = 3; startColumn = 4 } } }) }) }) } |
  ConvertTo-Json -Depth 10 | Set-Content (Join-Path $staleFolder 'Old.sarif')
(Get-Item (Join-Path $staleFolder 'Old.sarif')).LastWriteTimeUtc = [DateTime]::UtcNow.AddHours(-1)
$staleFail = Invoke-Area $staleRepo 'backend' @{ FAKE_BUILD_FAIL = $staleRepo }
Assert-That ($staleFail.Exit -eq 1 -and ($staleFail.Output -contains 'backend/App/A.cs(7,9): error CS0103: The name x does not exist') -and
  -not ($staleFail.Output -match 'An old error')) 'a failing build does not print the results of an old SARIF file'

$sharedScripts = Join-Path $work 'shared-area-scripts'
New-Item -ItemType Directory -Force (Join-Path $sharedScripts 'hooks') | Out-Null
foreach ($script in 'run-area.ps1', 'tree-id.ps1') { Copy-Item (Join-Path $ScriptsDir $script) $sharedScripts }
$sharedHelpers = (Get-Content (Join-Path $ScriptsDir 'hooks/checkout-helpers.ps1') -Raw).Replace("'keycloak-theme'", "'keycloak-theme', 'extra-front'")
Set-Content (Join-Path $sharedScripts 'hooks/checkout-helpers.ps1') $sharedHelpers -NoNewline
$sharedRepo = New-AreaRepo 'ra-shared-paths'
$sharedTreeBefore = (& pwsh -NoProfile -File (Join-Path $sharedScripts 'tree-id.ps1') -Repo $sharedRepo -Area frontend 2>&1) -join ''
New-Item -ItemType Directory -Force (Join-Path $sharedRepo 'extra-front') | Out-Null
Set-Content (Join-Path $sharedRepo 'extra-front/e.ts') 'export const e = 1;'
$sharedTreeAfter = (& pwsh -NoProfile -File (Join-Path $sharedScripts 'tree-id.ps1') -Repo $sharedRepo -Area frontend 2>&1) -join ''
$sharedLog = Join-Path $work 'ra-shared.log'
New-Item -ItemType File $sharedLog | Out-Null
$env:FAKE_LOG = $sharedLog
try {
  $sharedOutput = & pwsh -NoProfile -File (Join-Path $sharedScripts 'run-area.ps1') -Area frontend -Repo $sharedRepo -Base base -FormatScript $areaFormat 2>&1
}
finally { Remove-Item Env:FAKE_LOG -ErrorAction SilentlyContinue }
$sharedTreeRun = (& pwsh -NoProfile -File (Join-Path $sharedScripts 'tree-id.ps1') -Repo $sharedRepo -Area frontend 2>&1) -join ''
Assert-That ($sharedTreeBefore -ne $sharedTreeAfter -and (@(Get-Content $sharedLog) -contains 'format extra-front/e.ts,frontend/app.ts') -and
  ("$(@($sharedOutput)[-1])" -eq "Full run frontend: tree $sharedTreeRun; lint, ng build, ng test -> 3 passed, 0 failed")) `
  'a path added only to the shared frontend path list changes the tree ID and the files of the run'


$namedRun = Invoke-Area (New-AreaRepo 'ra-named' @{ NamedIndex = $true }) 'backend'
Assert-That ($namedRun.Exit -eq 1 -and ($namedRun.Output -contains 'src/b.cs(2,14): error NAMED_INDEX: the index access names its argument; write list[0], not list[index: 0].') -and
  $namedRun.Last -like 'RUN FAILED backend:*' -and ($namedRun.Log -notcontains 'dotnet build')) 'a named index argument fails the run before the build'

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





# --- tooling area (Step 86): New-ToolingRepo is defined with the shared fake tools ---

$rulesRepo = New-ToolingRepo 'ra-tooling-rules' @('.claude/rules/sample.md')
$rulesRun = Invoke-Area $rulesRepo 'tooling'
$rulesTree = Get-AreaTreeId $rulesRepo 'tooling'
Assert-That ($rulesRun.Exit -eq 0 -and $rulesRun.Last -eq "Full run tooling: tree $rulesTree; suites rules.tests.ps1 -> passed") `
  'a changed .claude/rules/ file runs only rules.tests.ps1 and prints the Full run tooling line'
Assert-That (($rulesRun.Log -join '|') -eq 'suite rules.tests.ps1') 'a tooling run starts no format step and no build, only the matching suite'

foreach ($rulesCase in @(@('ra-tooling-agent', '.claude/agents/executor.md'), @('ra-tooling-command', '.claude/commands/spec.md'))) {
  $rulesFileRun = Invoke-Area (New-ToolingRepo $rulesCase[0] @($rulesCase[1])) 'tooling'
  Assert-That ($rulesFileRun.Exit -eq 0 -and ($rulesFileRun.Log -join '|') -eq 'suite rules.tests.ps1') `
    "a changed $($rulesCase[1]) runs rules.tests.ps1"
}
$backendOnlyRun = Invoke-Area (New-ToolingRepo 'ra-tooling-backend' @('src/b.cs')) 'tooling'
Assert-That ($backendOnlyRun.Exit -eq 0 -and $backendOnlyRun.Last -like 'Full run tooling: tree *; suites none -> passed' -and $backendOnlyRun.Log.Count -eq 0) `
  'a changed backend file runs no tool suite'
$sharedHelperRun = Invoke-Area (New-ToolingRepo 'ra-tooling-helper' @('.claude/hooks/checkout-helpers.ps1')) 'tooling'
Assert-That ($sharedHelperRun.Exit -eq 0 -and (($sharedHelperRun.Log | Sort-Object) -join '|') -eq 'suite flow-scripts.tests.ps1|suite hook-scope.tests.ps1|suite rules.tests.ps1') `
  'a changed shared hook helper (checkout-helpers.ps1) runs hook-scope.tests.ps1 and flow-scripts.tests.ps1 (and rules.tests.ps1, which reads every .ps1 of .claude/)'
$otherHookRun = Invoke-Area (New-ToolingRepo 'ra-tooling-hook' @('.claude/hooks/verify-changed.ps1')) 'tooling'
Assert-That ($otherHookRun.Exit -eq 0 -and (($otherHookRun.Log | Sort-Object) -join '|') -eq 'suite hook-scope.tests.ps1|suite rules.tests.ps1') `
  'a changed hook file that no script dot-sources (verify-changed.ps1) runs hook-scope.tests.ps1 and rules.tests.ps1, not flow-scripts.tests.ps1'

$noSuiteRun = Invoke-Area (New-ToolingRepo 'ra-tooling-none' @('.claude/commands/note.txt')) 'tooling'
Assert-That ($noSuiteRun.Exit -eq 0 -and $noSuiteRun.Last -like 'Full run tooling: tree *; suites none -> passed' -and $noSuiteRun.Log.Count -eq 0) `
  'a tooling run with no matching pattern runs no suite and says suites none'

$suiteFail = Invoke-Area (New-ToolingRepo 'ra-tooling-fail' @('.claude/hooks/x.ps1')) 'tooling' @{ FAKE_SUITE_FAIL = 'hook-scope.tests.ps1' }
Assert-That ($suiteFail.Exit -eq 1 -and $suiteFail.Last -eq 'RUN FAILED tooling: hook-scope.tests.ps1 - FAIL a sample check') `
  'a failing suite prints RUN FAILED tooling with the suite and its first FAIL line'

}

# --- run-tests -------------------------------------------------------------------------------------------------
if (Test-Section 'run-tests') {
Write-Host 'run-tests'

# Three features: Alpha (backend src/Alpha, frontend frontend/src/app/alpha), Beta and Gamma (backend only; none in the
# frontend). The fake dotnet and npx log every argument line to the log file; the fake npx also logs HF_TEST_TAGS.
function New-TestsRepo([string] $name, [string[]] $changed) {
  $dir = New-Repo $name
  Set-Content (Join-Path $dir 'test-features.json') (@{ features = @(
        @{ name = 'Alpha'; backend = @('src/Alpha/**'); frontend = @('frontend/src/app/alpha/**') },
        @{ name = 'Beta'; backend = @('src/Beta/**'); frontend = @() },
        @{ name = 'Gamma'; backend = @('src/Gamma/**'); frontend = @() }) } | ConvertTo-Json -Depth 5)
  foreach ($path in $changed) {
    New-Item -ItemType Directory -Force (Split-Path (Join-Path $dir $path) -Parent) | Out-Null
    Set-Content (Join-Path $dir $path) 'base'
  }
  Save-Commit $dir 'base'
  git -C $dir tag base
  foreach ($path in $changed) { Add-Content (Join-Path $dir $path) '// edit' }
  return $dir
}

$testsFormat = Join-Path $work 'fake-tests-format.ps1'
Set-Content $testsFormat 'param([string]$Repo, [string[]]$Files)'

function Invoke-Tests([string] $repoDir, [string[]] $arguments, [hashtable] $environment = @{}) {
  $log = Join-Path $work "tests-$([Guid]::NewGuid().ToString('N').Substring(0, 8)).log"
  New-Item -ItemType File $log | Out-Null
  $env:FAKE_LOG = $log
  $env:FAKE_ARGS_LOG = $log
  foreach ($key in $environment.Keys) { Set-Item "Env:$key" $environment[$key] }
  try { $result = Invoke-Script 'run-tests.ps1' (@('-Repo', $repoDir, '-Base', 'base', '-FormatScript', $testsFormat) + $arguments) }
  finally {
    Remove-Item Env:FAKE_LOG, Env:FAKE_ARGS_LOG -ErrorAction SilentlyContinue
    foreach ($key in $environment.Keys) { Remove-Item "Env:$key" -ErrorAction SilentlyContinue }
  }
  [pscustomobject]@{ Output = $result.Output; Exit = $result.Exit; Log = @(Get-Content $log); Last = @($result.Output)[-1] }
}

$mappedRepo = New-TestsRepo 'rt-mapped' @('src/Alpha/a.cs')
$mappedRun = Invoke-Tests $mappedRepo @('-Area', 'backend', '-Kind', 'integration', '-Changed')
$mappedTree = Get-AreaTreeId $mappedRepo 'backend'
Assert-That ($mappedRun.Exit -eq 0 -and $mappedRun.Last -eq
  "Affected run backend: tree $mappedTree; features Alpha; build, integration+API command -> 7 passed, 0 failed") `
  'a mapped backend path runs the integration command of its feature and prints the Affected run line'
Assert-That (@($mappedRun.Log | Where-Object { $_ -like 'dotnet test*--filter-trait Feature=Alpha*' }).Count -eq 1 -and
  @($mappedRun.Log | Where-Object { $_ -like '*Feature=Beta*' }).Count -eq 0) 'a mapped path passes --filter-trait of its feature only'

$unmappedRepo = New-TestsRepo 'rt-unmapped' @('src/Other/x.cs')
$unmappedRun = Invoke-Tests $unmappedRepo @('-Area', 'backend', '-Kind', 'integration', '-Changed')
Assert-That ($unmappedRun.Exit -eq 0 -and $unmappedRun.Last -like 'Affected run backend: tree *; features all; *' -and
  @($unmappedRun.Log | Where-Object { $_ -like '*--filter-trait*' }).Count -eq 0) `
  'an unmapped backend path selects every feature and passes no --filter-trait'
Assert-That (@($mappedRun.Log | Where-Object { $_ -like 'dotnet test*--filter-trait Feature=Alpha*--ignore-exit-code 8*' }).Count -eq 1 -and
  @($unmappedRun.Log | Where-Object { $_ -like '*--ignore-exit-code*' }).Count -eq 0) `
  'an integration command with --filter-trait also passes --ignore-exit-code 8 (a module without a test of the feature exits 8); without a filter it passes none'

$unitRun = Invoke-Tests $mappedRepo @('-Area', 'backend', '-Kind', 'unit', '-Changed')
Assert-That ($unitRun.Exit -eq 0 -and $unitRun.Log -contains 'dotnet unit' -and $unitRun.Log -notcontains 'dotnet integration' -and
  @($unitRun.Log | Where-Object { $_ -like '*--filter-trait*' }).Count -eq 0) '-Kind unit starts no integration command and no trait filter'

$unitFailRun = Invoke-Tests $mappedRepo @('-Area', 'backend', '-Changed') @{ FAKE_UNIT_FAIL = '1' }
Assert-That ($unitFailRun.Exit -eq 1 -and $unitFailRun.Last -eq 'RUN FAILED backend: unit+architecture command failed' -and
  $unitFailRun.Log -notcontains 'dotnet integration') 'a failing unit command starts no integration command'

$featureRun = Invoke-Tests $mappedRepo @('-Area', 'backend', '-Kind', 'integration', '-Feature', 'Beta,Alpha')
Assert-That ($featureRun.Exit -eq 0 -and $featureRun.Last -like '*features Beta, Alpha;*' -and
  @($featureRun.Log | Where-Object { $_ -like '*--filter-trait Feature=Beta*--filter-trait Feature=Alpha*' }).Count -eq 1) `
  '-Feature selects the named features, one --filter-trait each'

$projectRun = Invoke-Tests $mappedRepo @('-Area', 'backend', '-Project', 'backend/tests/HealthyFac.Core.UnitTests', '-Filter', '/*/*/*/Sample*')
Assert-That ($projectRun.Exit -eq 0 -and
  @($projectRun.Log | Where-Object { $_ -like 'dotnet test --project backend/tests/HealthyFac.Core.UnitTests --no-build --filter /*/*/*/Sample*' }).Count -eq 1) `
  '-Project runs one project without a build and with the filter'

$frontendRepo = New-TestsRepo 'rt-frontend' @('frontend/src/app/alpha/x.ts')
$frontendRun = Invoke-Tests $frontendRepo @('-Area', 'frontend', '-Changed')
Assert-That ($frontendRun.Exit -eq 0 -and @($frontendRun.Log | Where-Object { $_ -like 'npx ng test --watch=false --include src/app/alpha @ frontend' }).Count -eq 1 -and
  $frontendRun.Log -contains 'tags Alpha') 'a frontend path of a feature gives --include of its folder and HF_TEST_TAGS'

$sharedRepo = New-TestsRepo 'rt-shared' @('frontend/src/app/shared/x/y.ts')
$sharedRun = Invoke-Tests $sharedRepo @('-Area', 'frontend', '-Changed')
Assert-That ($sharedRun.Exit -eq 0 -and @($sharedRun.Log | Where-Object { $_ -like 'npx ng test --watch=false @ frontend' }).Count -eq 1 -and
  @($sharedRun.Log | Where-Object { $_ -like 'tags *' }).Count -eq 0) 'a frontend shared/ path gives neither --include nor HF_TEST_TAGS'

$noFrontendRun = Invoke-Tests $mappedRepo @('-Area', 'frontend', '-Feature', 'Beta')
Assert-That ($noFrontendRun.Exit -eq 0 -and @($noFrontendRun.Log | Where-Object { $_ -like 'npx ng test*' }).Count -eq 0 -and
  $noFrontendRun.Last -like 'Affected run frontend: tree *; features Beta; no frontend tests -> 0 passed, 0 failed') `
  'a feature without frontend globs starts no frontend command'


function Read-StoredResult([string] $repoDir, [string] $area, [string] $mode) {
  $path = Join-Path $repoDir ".git/claude-runs/result-$area-$mode.json"
  if (Test-Path $path) { Get-Content $path -Raw | ConvertFrom-Json }
}

$passResult = Read-StoredResult $mappedRepo 'backend' 'affected'
Assert-That ($passResult -and $passResult.kind -eq 'Affected run' -and $passResult.status -eq 'pass' -and $passResult.tree -eq $mappedTree -and
  $passResult.line -like 'Affected run backend: tree *' -and $passResult.finished -is [datetime]) `
  'a passing run-tests run stores a pass result with the kind, the tree, and the last line'
$null = Invoke-Tests $mappedRepo @('-Area', 'backend', '-Kind', 'unit', '-Changed') @{ FAKE_UNIT_FAIL = '1' }
$failResult = Read-StoredResult $mappedRepo 'backend' 'affected'
Assert-That ($failResult -and $failResult.status -eq 'fail' -and $failResult.line -eq 'RUN FAILED backend: unit+architecture command failed') `
  'a failing run-tests run stores a fail result with the RUN FAILED line'

$resultFormat = Join-Path $work 'fake-result-format.ps1'
Set-Content $resultFormat 'param([string]$Repo, [string[]]$Files)'
function Invoke-FullRun([string] $repoDir, [hashtable] $environment = @{}) {
  $env:FAKE_LOG = Join-Path $work "full-$([Guid]::NewGuid().ToString('N').Substring(0, 8)).log"
  foreach ($key in $environment.Keys) { Set-Item "Env:$key" $environment[$key] }
  try { $result = Invoke-Script 'run-area.ps1' @('-Area', 'backend', '-Repo', $repoDir, '-Base', 'base', '-FormatScript', $resultFormat) }
  finally {
    Remove-Item Env:FAKE_LOG -ErrorAction SilentlyContinue
    foreach ($key in $environment.Keys) { Remove-Item "Env:$key" -ErrorAction SilentlyContinue }
  }
  $result
}
$fullRepo = New-TestsRepo 'rt-full' @('src/Alpha/a.cs')
$fullRun = Invoke-FullRun $fullRepo
$fullResult = Read-StoredResult $fullRepo 'backend' 'full'
Assert-That ($fullRun.Exit -eq 0 -and $fullResult -and $fullResult.kind -eq 'Full run' -and $fullResult.status -eq 'pass' -and
  $fullResult.line -eq @($fullRun.Output)[-1] -and $fullResult.tree -eq (Get-AreaTreeId $fullRepo 'backend')) `
  'a passing run-area run stores a pass result of the kind Full run'
$null = Invoke-FullRun $fullRepo @{ FAKE_UNIT_FAIL = '1' }
$fullFail = Read-StoredResult $fullRepo 'backend' 'full'
Assert-That ($fullFail -and $fullFail.status -eq 'fail' -and $fullFail.line -eq 'RUN FAILED backend: unit+architecture command failed') `
  'a failing run-area run stores a fail result'

$toolRepo = New-ToolingRepo 'rt-tooling' @('.claude/rules/sample.md')
$toolRun = Invoke-Tests $toolRepo @('-Area', 'tooling', '-Changed')
$toolTree = Get-AreaTreeId $toolRepo 'tooling'
Assert-That ($toolRun.Exit -eq 0 -and $toolRun.Last -eq "Affected run tooling: tree $toolTree; suites rules.tests.ps1 -> passed" -and
  ($toolRun.Log -join '|') -eq 'suite rules.tests.ps1') 'a changed .claude/rules/ file runs only rules.tests.ps1 through run-tests'
$toolResult = Read-StoredResult $toolRepo 'tooling' 'affected'
Assert-That ($toolResult -and $toolResult.status -eq 'pass' -and $toolResult.tree -eq $toolTree) 'a tooling run stores its result'
$toolFail = Invoke-Tests (New-ToolingRepo 'rt-tooling-fail' @('.claude/hooks/x.ps1')) @('-Area', 'tooling', '-Changed') @{ FAKE_SUITE_FAIL = 'hook-scope.tests.ps1' }
Assert-That ($toolFail.Exit -eq 1 -and $toolFail.Last -eq 'RUN FAILED tooling: hook-scope.tests.ps1 - FAIL a sample check') `
  'a failing tool suite prints RUN FAILED tooling with the suite and its first FAIL line'

$showFresh = Invoke-Script 'run-tests.ps1' @('-ShowResults', '-Repo', $fullRepo)
Assert-That ($showFresh.Exit -eq 0 -and ($showFresh.Output | Where-Object { $_ -like 'Full run backend: tree *; fail; current' }).Count -eq 1) `
  '-ShowResults prints one line for each stored result, current for the tree of the area now'
$null = Invoke-FullRun $fullRepo
Add-Content (Join-Path $fullRepo 'src/Alpha/a.cs') '// later edit'
$showStale = Invoke-Script 'run-tests.ps1' @('-ShowResults', '-Repo', $fullRepo)
Assert-That (($showStale.Output | Where-Object { $_ -like 'Full run backend: tree *; pass; stale' }).Count -eq 1) `
  '-ShowResults marks a result stale after a change of its area'

Push-Location $fullRepo
try { $showRelative = Invoke-Script 'run-tests.ps1' @('-ShowResults', '-Repo', '.') } finally { Pop-Location }
Assert-That ($showRelative.Exit -eq 0 -and ($showRelative.Output | Where-Object { $_ -like 'Full run backend: tree *; pass; stale' }).Count -eq 1) `
  '-ShowResults -Repo . run from the root of the checkout prints the stored results and exits 0'

# --- hook state after a run (Step 80) ---
function New-StateRepo([string] $name) {
  $dir = New-Repo $name
  New-Item -ItemType Directory -Force (Join-Path $dir 'backend/App') | Out-Null
  Set-Content (Join-Path $dir 'test-features.json') (@{ features = @(@{ name = 'Alpha'; backend = @('backend/App/**'); frontend = @() }) } | ConvertTo-Json -Depth 5)
  Set-Content (Join-Path $dir '.gitignore') 'obj/'
  Set-Content (Join-Path $dir 'backend/App/A.cs') 'class A {}'
  Save-Commit $dir 'base'
  git -C $dir tag base
  Add-Content (Join-Path $dir 'backend/App/A.cs') '// edit'
  return $dir
}

$stateRepo = New-StateRepo 'rt-state'
$stateRun = Invoke-Tests $stateRepo @('-Area', 'backend', '-Kind', 'unit', '-Changed')
$statePath = Join-Path $stateRepo '.git/claude-verify-backend-tree.txt'
Assert-That ($stateRun.Exit -eq 0 -and (Test-Path $statePath) -and (Get-Content $statePath -Raw).Trim() -eq (Get-TreeId $stateRepo)) `
  'a passing backend run-tests run writes the hook state of the backend step with the current tree'
$stateFullRepo = New-StateRepo 'rt-state-full'
$null = Invoke-FullRun $stateFullRepo
Assert-That ((Test-Path (Join-Path $stateFullRepo '.git/claude-verify-backend-tree.txt')) -and
  (Get-Content (Join-Path $stateFullRepo '.git/claude-verify-backend-tree.txt') -Raw).Trim() -eq (Get-TreeId $stateFullRepo)) `
  'a passing run-area backend run writes the hook state too'

$warnRepo = New-StateRepo 'rt-state-warn'
$warnRun = Invoke-Tests $warnRepo @('-Area', 'backend', '-Kind', 'unit', '-Changed') @{ FAKE_BUILD_WARN = $warnRepo }
$warnResult = Read-StoredResult $warnRepo 'backend' 'affected'
Assert-That ($warnRun.Exit -eq 0 -and @($warnRun.Output | Where-Object { $_ -like 'Hook check: backend/App/A.cs(3,5): warning CS0219:*' }).Count -eq 1 -and
  -not (Test-Path (Join-Path $warnRepo '.git/claude-verify-backend-tree.txt')) -and $warnResult.status -eq 'pass' -and
  $warnRun.Last -like 'Affected run backend:*') 'a SARIF warning in a changed file prints Hook check: and writes no state; the run result stays pass'

$namedRepo = New-StateRepo 'rt-state-named'
Add-Content (Join-Path $namedRepo 'backend/App/A.cs') 'class B { int F(int[] list) => list[index: 0]; }'
$namedRun = Invoke-Tests $namedRepo @('-Area', 'backend', '-Kind', 'unit', '-Changed')
$namedResult = Read-StoredResult $namedRepo 'backend' 'affected'
Assert-That ($namedRun.Exit -eq 0 -and
  @($namedRun.Output | Where-Object { $_ -like 'Hook check: backend/App/A.cs(*): error NAMED_INDEX:*' }).Count -eq 1 -and
  -not (Test-Path (Join-Path $namedRepo '.git/claude-verify-backend-tree.txt')) -and $namedResult.status -eq 'pass' -and
  $namedRun.Last -like 'Affected run backend:*') `
  'a named index argument in a changed file prints Hook check: and writes no backend state; the run result stays pass'

$stateFailRepo = New-StateRepo 'rt-state-fail'
$null = Invoke-Tests $stateFailRepo @('-Area', 'backend', '-Kind', 'unit', '-Changed') @{ FAKE_UNIT_FAIL = '1' }
Assert-That (-not (Test-Path (Join-Path $stateFailRepo '.git/claude-verify-backend-tree.txt'))) 'a failing run writes no hook state'

# --- keycloak theme in the frontend run (Step 107) ---
$themeRun = Invoke-Tests (New-TestsRepo 'rt-theme' @('keycloak-theme/src/x.ts', 'frontend/src/app/alpha/y.ts')) @('-Area', 'frontend', '-Changed')
Assert-That ($themeRun.Exit -eq 0 -and ($themeRun.Log -join '|') -like '*npx ng test*|npm run lint @ keycloak-theme|npm test @ keycloak-theme*' -and
  $themeRun.Last -like '*keycloak-theme lint, keycloak-theme test*') 'a changed keycloak-theme path runs npm run lint and npm test in keycloak-theme/ after ng test'
$themeFailRepo = New-TestsRepo 'rt-theme-fail' @('keycloak-theme/src/x.ts', 'frontend/src/app/alpha/y.ts')
$themeFail = Invoke-Tests $themeFailRepo @('-Area', 'frontend', '-Changed') @{ FAKE_NPM_FAIL = 'run lint' }
$themeFailResult = Read-StoredResult $themeFailRepo 'frontend' 'affected'
Assert-That ($themeFail.Exit -eq 1 -and $themeFail.Last -eq 'RUN FAILED frontend: keycloak-theme lint failed' -and $themeFailResult.status -eq 'fail') `
  'a failing npm run lint in keycloak-theme/ prints RUN FAILED frontend and stores no pass'
$plainFrontend = Invoke-Tests (New-TestsRepo 'rt-theme-none' @('frontend/src/app/alpha/x.ts')) @('-Area', 'frontend', '-Changed')
Assert-That ($plainFrontend.Exit -eq 0 -and @($plainFrontend.Log | Where-Object { $_ -like '*@ keycloak-theme' }).Count -eq 0) `
  'a change only under frontend/ runs no keycloak command'
}

if ($previousPath) { $env:PATH = $previousPath }

Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
if ($failures.Count -gt 0) {
  Write-Host "`n$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host "`nAll checks passed ($($ranSections -join ', '))."
