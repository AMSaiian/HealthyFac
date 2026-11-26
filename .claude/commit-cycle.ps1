#Requires -Version 7
# commit-cycle.ps1 - commits an approved cycle as one commit for each group of its changed files: tooling, docs, and
# code (.claude/agents/executor.md, Commit). The subjects come from the "Commit <group>: <subject>" lines of the
# cycle in plan.md (.claude/commands/spec.md, plan.md). A change of code from an earlier commit of the branch is a
# fixup: it is committed with "git commit --fixup" and folded by the autosquash (.claude/rules/git-commits.md, History).
# Usage: pwsh .claude/commit-cycle.ps1 <feature folder> [-Repo <path>] [-MainBranch <name>] [-Verify | -Squash]
# Defaults: -Repo is the checkout that holds this script file (not the current folder); -MainBranch is develop.
# Checks, all before the first commit (each failure is one line, and the script makes no commit):
# - plan.md and review.md exist in the folder;
# - plan.md has a "Cycle: N" line and its "### Cycle N" section;
# - review.md has "Verdict: APPROVE", its Cycle line equals N, and its "Reviewed tree:" equals the tree of the checkout
#   (tree-id.ps1), so a change after the review stops the commit;
# - the Commit lines of the cycle have no problem (plan-sections.ps1, Get-CommitProblems); a cycle without a Commit line
#   is allowed only when Test-FixupOnlyCycle is true (1 or more fixup entries, and each step with files has a
#   "Fixup:" line; a step without files does not count), and each changed path that is no fixup path still needs a
#   Commit line of its group;
# - no path is in two fixup entries with different subjects, no path of an inline fix is in a step without
#   "Fixup:", and no path of a "Fixup:" step is also in a step without "Fixup:" (Get-FixupPathProblems);
# - when the cycle is the last one (no "### Cycle N+1" section), no browser check step of plan.md is "[!]" with
#   "WAITING:" or "BLOCKED:": the last cycle commits after its flows pass;
# - each "Fixup: <subject>" of a step or of an "Inline fix" entry of the cycle matches exactly 1 commit subject of
#   <base>..HEAD (Get-FeatureBase: the "Base:" SHA of plan.md, else the merge base of HEAD and origin/<main branch>, or
#   the local main branch when origin has none);
# - each other changed path (git diff --name-only --no-renames HEAD, so a move lists both paths, plus the untracked
#   files) has a Commit line for its group;
# - a Commit line whose group has no changed path is done only when a commit since the feature base (Get-FeatureBase:
#   the "Base:" SHA of plan.md, else Get-DefaultBase) has its subject.
# Commit, in this order: for each fixup entry that has a changed file, in step order, "git add -A" and
# "git commit --fixup=<SHA> --only" on its changed files; then, for each Commit line that is not done, in the order
# tooling, docs, code: "git add -A" and "git commit -m <subject> --only" on the paths of its group. No body, no
# trailer, no --no-verify, no --author. The script never pushes, amends, or resets. A rerun skips the lines that are
# done.
# Autosquash: then "git rebase --autosquash <squash base>" (Git 2.44 or later; else "git rebase -i --autosquash" with
# GIT_SEQUENCE_EDITOR=true). The squash base is the "Reviewed by Artur:" SHA of plan.md, else its "Base:" SHA, else the
# default base (Get-DefaultBase). The tree of HEAD before the rebase and the squash base are stored in the
# git directory (claude-autosquash-tree.txt; -Verify reads both, and reports "no stored autosquash tree." without
# the file). The rebase runs with --no-autostash. Two checks follow: the HEAD tree is the stored tree, and no "fixup!" commit of
# <squash base>..HEAD has its target above the squash base. On a conflict the script prints
# "autosquash conflict: <path>" for each file, leaves the rebase in progress, and exits 1.
# -Verify: no commit and no rebase; it runs the two checks (after a manual "git -c core.editor=true rebase --continue").
# -Squash: no commit; it stops with "the checkout is not clean." on a dirty checkout, else runs the autosquash onto the
# "Base:" SHA, else the merge base (never the reviewed SHA), then the two checks. It folds the fixups below the
# reviewed SHA after Artur's "review finished".
# Output: one line "<short SHA> <subject>" for each commit, read from the history after the autosquash (the rebase
# rewrites the SHAs above a folded target), or one line for each problem.
# Exit code 1 on a problem, on a failing git call, or when the checkout is not clean after the last commit.
param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$MainBranch = 'develop',
  [switch]$Verify,
  [switch]$Squash
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'plan-sections.ps1')
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$GroupOrder = @('tooling', 'docs', 'code')
$TreeFileName = 'claude-autosquash-tree.txt'

function Stop-WithLines([string[]] $Lines) {
  $Lines | ForEach-Object { Write-Host $_ }
  exit 1
}

function Invoke-Git([string[]] $Arguments) {
  $output = & git -C $Repo -c core.quotepath=false @Arguments 2>&1
  if ($LASTEXITCODE -ne 0) { Stop-WithLines @("git $($Arguments -join ' ') failed: $(($output | ForEach-Object { "$_" }) -join ' ')") }
  return @($output | ForEach-Object { "$_" })
}

function Get-GitDirectory { return @(Invoke-Git @('rev-parse', '--absolute-git-dir'))[0] }

# The "fixup!" commits of <base>..HEAD whose target subject belongs to a commit of the same range.
function Get-StrandedFixups([string] $SquashBase) {
  $subjects = @(Invoke-Git @('log', '--format=%h %s', "$SquashBase..HEAD"))
  $plain = @($subjects | ForEach-Object { $_.Substring($_.IndexOf(' ') + 1) } | Where-Object { $_ -notmatch '^fixup! ' })
  foreach ($entry in $subjects) {
    $subject = $entry.Substring($entry.IndexOf(' ') + 1)
    if ($subject -notmatch '^fixup! (.+)$') { continue }
    if ($Matches[1] -in $plain) { "a fixup! commit stays above ${SquashBase}: $entry" }
  }
}

# The problem line when the squash base is no ancestor of HEAD (the rebase would move the branch onto another
# history), else $null.
function Get-SquashBaseProblem([string] $SquashBase) {
  & git -C $Repo merge-base --is-ancestor $SquashBase HEAD 2>&1 | Out-Null
  if ($LASTEXITCODE -eq 0) { return $null }
  "the squash base $($SquashBase.Substring(0, [Math]::Min(7, $SquashBase.Length))) is no ancestor of HEAD."
}

# Stops with the problem line of Get-SquashBaseProblem.
function Assert-SquashBaseIsAncestor([string] $SquashBase) {
  $problem = Get-SquashBaseProblem $SquashBase
  if ($problem) { Stop-WithLines @($problem) }
}

# Reads the tree and the squash base that Invoke-Autosquash stored before its rebase.
function Test-AutosquashResult {
  $problems = [Collections.Generic.List[string]]::new()
  $treeFile = Join-Path (Get-GitDirectory) $TreeFileName
  if (-not (Test-Path -LiteralPath $treeFile)) { Stop-WithLines @('no stored autosquash tree.') }
  $stored = @(Get-Content -LiteralPath $treeFile)
  $before = $stored[0]
  $SquashBase = $stored[1]
  Assert-SquashBaseIsAncestor $SquashBase
  $after = @(Invoke-Git @('rev-parse', 'HEAD^{tree}'))[0]
  if ($before -ne $after) { $problems.Add('the HEAD tree changed in the autosquash.') }
  foreach ($line in @(Get-StrandedFixups $SquashBase)) { $problems.Add($line) }
  if ($problems.Count -gt 0) { Stop-WithLines $problems }
}

function Invoke-Autosquash([string] $SquashBase) {
  $treeFile = Join-Path (Get-GitDirectory) $TreeFileName
  [IO.File]::WriteAllText($treeFile, "$(@(Invoke-Git @('rev-parse', 'HEAD^{tree}'))[0])`n$SquashBase`n")
  $version = [regex]::Match((git --version), '(\d+)\.(\d+)')
  $isModern = [int]$version.Groups[1].Value -gt 2 -or ([int]$version.Groups[1].Value -eq 2 -and [int]$version.Groups[2].Value -ge 44)
  $previousSequenceEditor = $env:GIT_SEQUENCE_EDITOR
  $previousEditor = $env:GIT_EDITOR
  $env:GIT_EDITOR = 'true'
  try {
    if ($isModern) {
      $output = & git -C $Repo rebase --no-autostash --autosquash $SquashBase 2>&1
    } else {
      $env:GIT_SEQUENCE_EDITOR = 'true'
      $output = & git -C $Repo rebase -i --no-autostash --autosquash $SquashBase 2>&1
    }
    $exit = $LASTEXITCODE
  } finally {
    $env:GIT_SEQUENCE_EDITOR = $previousSequenceEditor
    $env:GIT_EDITOR = $previousEditor
  }
  if ($exit -eq 0) { return }
  $conflicts = @(& git -C $Repo -c core.quotepath=false diff --name-only --diff-filter=U)
  if ($conflicts.Count -eq 0) { Stop-WithLines @("git rebase --autosquash failed: $(($output | ForEach-Object { "$_" }) -join ' ')") }
  $lines = @($conflicts | ForEach-Object { "autosquash conflict: $_" })
  $lines += 'resolve, git add, git -c core.editor=true rebase --continue, then run commit-cycle.ps1 -Verify'
  Stop-WithLines $lines
}

$planPath = Join-Path $Folder 'plan.md'
$reviewPath = Join-Path $Folder 'review.md'
$requiredPaths = if ($Verify -or $Squash) { @($planPath) } else { @($planPath, $reviewPath) }
foreach ($path in $requiredPaths) {
  if (-not (Test-Path -LiteralPath $path)) { Stop-WithLines @("No file: $path") }
}
$planLines = Get-Content -LiteralPath $planPath

$featureBase = Get-FeatureBase -Repo $Repo -PlanLines $planLines -MainBranch $MainBranch
if (-not $featureBase) { Stop-WithLines @("no merge base of HEAD and $MainBranch.") }

if ($Verify -or $Squash) {
  if ($Squash) {
    if (@(Invoke-Git @('status', '--porcelain')).Count -gt 0) { Stop-WithLines @('the checkout is not clean.') }
    Assert-SquashBaseIsAncestor $featureBase
    Invoke-Autosquash $featureBase
  }
  Test-AutosquashResult
  exit 0
}

$reviewText = Get-Content -Raw -LiteralPath $reviewPath

$cycleLine = $planLines | Where-Object { $_ -match '^Cycle:\s*(\d+)\s*$' } | Select-Object -First 1
if (-not $cycleLine) { Stop-WithLines @('plan.md has no Cycle: line.') }
$cycle = [int]([regex]::Match($cycleLine, '^Cycle:\s*(\d+)').Groups[1].Value)
if (-not ($planLines | Where-Object { $_ -match "^### Cycle $cycle\s*$" })) {
  Stop-WithLines @("plan.md has no '### Cycle $cycle' section.")
}

$problems = [Collections.Generic.List[string]]::new()
$isLastCycle = -not ($planLines | Where-Object { $_ -match "^### Cycle $($cycle + 1)\s*$" })
if ($isLastCycle) {
  foreach ($heading in @($planLines | Where-Object { $_ -match '^### Cycle \d+\s*$' })) {
    $cycleNumber = [int][regex]::Match($heading, '\d+').Value
    foreach ($step in @(Get-CycleSteps (Get-PlanCycle $planLines $cycleNumber).Section)) {
      $kind = Get-BrowserCheckMark $step
      if ($kind) { $problems.Add("Step $($step.Number) is a $kind browser check: the last cycle commits after its flows pass.") }
    }
  }
}
$reviewedSha = Get-ReviewedMark $planLines
$squashBase = if ($reviewedSha) { $reviewedSha } else { $featureBase }
$squashBaseProblem = Get-SquashBaseProblem $squashBase
if ($squashBaseProblem) { $problems.Add($squashBaseProblem) }

if ($reviewText -notmatch '(?m)^Verdict:\s*APPROVE\s*$') { $problems.Add('review.md has no Verdict: APPROVE.') }
$reviewCycle = [regex]::Match($reviewText, '(?m)^Cycle:\s*(\d+)\s*$')
if (-not $reviewCycle.Success -or [int]$reviewCycle.Groups[1].Value -ne $cycle) {
  $found = if ($reviewCycle.Success) { $reviewCycle.Groups[1].Value } else { 'none' }
  $problems.Add("review.md is for cycle $found, plan.md is at cycle $cycle.")
}
$reviewedTree = [regex]::Match($reviewText, '(?m)^Reviewed tree:\s*([0-9a-f]{40})\s*$')
$currentTree = (& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'tree-id.ps1') -Repo $Repo 2>&1 | ForEach-Object { "$_" }) -join ''
if (-not $reviewedTree.Success -or $reviewedTree.Groups[1].Value -ne $currentTree.Trim()) {
  $reviewed = if ($reviewedTree.Success) { $reviewedTree.Groups[1].Value } else { 'none' }
  $problems.Add("the checkout tree $($currentTree.Trim()) is not the Reviewed tree $reviewed.")
}

$section = (Get-PlanCycle $planLines $cycle).Section
$commits = @(Get-CycleCommits $section)
$steps = @(Get-CycleSteps $section)
$inlineFixups = @(Get-InlineFixups @(Get-CycleDeviations $section))
$fixups = @(Get-StepFixups $steps) + $inlineFixups
foreach ($text in @(Get-CommitProblems $commits -AllowNone:(Test-FixupOnlyCycle $steps $inlineFixups))) { $problems.Add($text) }
$loggedEntries = @(Invoke-Git @('log', '--format=%H%x09%s', "$featureBase..HEAD"))
$fixupShas = @{}
foreach ($text in @(Get-FixupPathProblems $cycle $steps $inlineFixups)) { $problems.Add($text) }
$loggedSubjects = @($loggedEntries | ForEach-Object { $_.Substring($_.IndexOf("`t") + 1) })
foreach ($text in @(Get-FixupSubjectProblems -Entries $fixups -Subjects $loggedSubjects -Base $featureBase)) { $problems.Add($text) }
foreach ($fixup in $fixups) {
  $matched = @($loggedEntries | Where-Object { $_.Substring($_.IndexOf("`t") + 1) -ceq $fixup.Subject })
  if ($matched.Count -eq 1 -and -not $fixupShas.ContainsKey($fixup.Subject)) {
    $fixupShas[$fixup.Subject] = $matched[0].Substring(0, $matched[0].IndexOf("`t"))
  }
}

$changed = @(Invoke-Git @('diff', '--name-only', '--no-renames', 'HEAD')) + @(Invoke-Git @('ls-files', '--others', '--exclude-standard'))
$changed = @($changed | Where-Object { $_ } | ForEach-Object { $_ -replace '\\', '/' } | Select-Object -Unique)
$fixupEntries = [Collections.Generic.List[object]]::new()
$claimedPaths = [Collections.Generic.HashSet[string]]::new()
foreach ($fixup in $fixups) {
  $own = @($fixup.Files | ForEach-Object { $_ -replace '\\', '/' } | Where-Object { $_ -in $changed -and $claimedPaths.Add($_) })
  if ($own.Count -gt 0 -and $fixupShas.ContainsKey($fixup.Subject)) {
    $fixupEntries.Add([pscustomobject]@{ Subject = $fixup.Subject; Files = $own })
  }
}
$groupChanged = @($changed | Where-Object { -not $claimedPaths.Contains($_) })
$pathsByGroup = @{}
foreach ($group in $GroupOrder) { $pathsByGroup[$group] = [Collections.Generic.List[string]]::new() }
foreach ($path in $groupChanged) { $pathsByGroup[(Get-CommitGroup $path)].Add($path) }

$isDone = @{}
foreach ($group in $GroupOrder) {
  $commit = $commits | Where-Object Group -eq $group | Select-Object -First 1
  if ($pathsByGroup[$group].Count -gt 0) {
    if (-not $commit) { foreach ($path in $pathsByGroup[$group]) { $problems.Add("$path needs a Commit $group line.") } }
  } elseif ($commit) {
    if ($commit.Subject -in $loggedSubjects) { $isDone[$group] = $true }
    else { $problems.Add("the Commit $group line has no changed files.") }
  }
}
if ($problems.Count -gt 0) { Stop-WithLines $problems }

function Save-Paths([string[]] $Paths, [string[]] $CommitArguments) {
  $listFile = Join-Path ([IO.Path]::GetTempPath()) "hf-commit-cycle-$([Guid]::NewGuid().ToString('N')).txt"
  $addFile = Join-Path ([IO.Path]::GetTempPath()) "hf-commit-cycle-$([Guid]::NewGuid().ToString('N')).txt"
  try {
    # A path that is gone from the work tree and the index (the old path of "git mv") has nothing to add: its deletion
    # is already staged, and "git commit --only" commits it.
    $toAdd = @($Paths | Where-Object {
        (Test-Path -LiteralPath (Join-Path $Repo $_)) -or @(Invoke-Git @('ls-files', '--', $_)).Count -gt 0 })
    [IO.File]::WriteAllLines($listFile, [string[]]$Paths)
    [IO.File]::WriteAllLines($addFile, [string[]]$toAdd)
    if ($toAdd.Count -gt 0) { [void](Invoke-Git @('add', '-A', "--pathspec-from-file=$addFile")) }
    [void](Invoke-Git (@('commit', '-q') + $CommitArguments + @('--only', "--pathspec-from-file=$listFile")))
  } finally {
    Remove-Item -LiteralPath $listFile, $addFile -Force -ErrorAction SilentlyContinue
  }
}

$committedSubjects = [Collections.Generic.List[string]]::new()
foreach ($entry in $fixupEntries) {
  Save-Paths $entry.Files @("--fixup=$($fixupShas[$entry.Subject])")
  $committedSubjects.Add("fixup! $($entry.Subject)")
}

foreach ($group in $GroupOrder) {
  $commit = $commits | Where-Object Group -eq $group | Select-Object -First 1
  if (-not $commit -or $isDone[$group] -or $pathsByGroup[$group].Count -eq 0) { continue }
  Save-Paths $pathsByGroup[$group] @('-m', $commit.Subject)
  $committedSubjects.Add($commit.Subject)
}

if (@(Invoke-Git @('status', '--porcelain')).Count -gt 0) { Stop-WithLines @('the checkout is not clean after the commits.') }

Invoke-Autosquash $squashBase
Test-AutosquashResult

# The rebase rewrites the commits above a folded target, so the SHAs are read from the final history.
$finalEntries = @(Invoke-Git @('log', '--format=%h %s', "$squashBase..HEAD"))
foreach ($subject in $committedSubjects) {
  $final = $finalEntries | Where-Object { $_.Substring($_.IndexOf(' ') + 1) -ceq $subject } | Select-Object -First 1
  if ($final) { Write-Host $final }
}
