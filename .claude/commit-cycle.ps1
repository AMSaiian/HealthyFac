#Requires -Version 7
# commit-cycle.ps1 - commits an approved cycle as one commit for each group of its changed files: tooling, docs, and
# code (.claude/agents/executor.md, Commit). The subjects come from the "Commit <group>: <subject>" lines of the
# cycle in plan.md (.claude/commands/spec.md, plan.md).
# Usage: pwsh .claude/commit-cycle.ps1 <feature folder> [-Repo <path>] [-MainBranch <name>]
# Defaults: -Repo is the checkout that holds this script file (not the current folder); -MainBranch is develop.
# Checks, all before the first commit (each failure is one line, and the script makes no commit):
# - plan.md and review.md exist in the folder;
# - plan.md has a "Cycle: N" line and its "### Cycle N" section;
# - review.md has "Verdict: APPROVE", its Cycle line equals N, and its "Reviewed tree:" equals the tree of the checkout
#   (tree-id.ps1), so a change after the review stops the commit;
# - the Commit lines of the cycle have no problem (plan-sections.ps1, Get-CommitProblems);
# - each changed path (git diff --name-only --no-renames HEAD, so a move lists both paths, plus the untracked files) has a Commit line for its group;
# - a Commit line whose group has no changed path is done only when a commit since the merge base of HEAD and the main
#   branch has its subject.
# Commit: for each line that is not done, in the order tooling, docs, code: "git add -A" and "git commit -m <subject>
# --only" on the paths of its group. No body, no trailer, no --no-verify, no --author. The script never pushes, amends,
# rebases, or resets. A rerun skips the lines that are done.
# Output: one line "<short SHA> <subject>" for each commit, or one line for each problem.
# Exit code 1 on a problem, on a failing git call, or when the checkout is not clean after the last commit.
param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$MainBranch = 'develop'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'plan-sections.ps1')
$GroupOrder = @('tooling', 'docs', 'code')

function Stop-WithLines([string[]] $Lines) {
  $Lines | ForEach-Object { Write-Host $_ }
  exit 1
}

function Invoke-Git([string[]] $Arguments) {
  $output = & git -C $Repo -c core.quotepath=false @Arguments 2>&1
  if ($LASTEXITCODE -ne 0) { Stop-WithLines @("git $($Arguments -join ' ') failed: $(($output | ForEach-Object { "$_" }) -join ' ')") }
  return @($output | ForEach-Object { "$_" })
}

$planPath = Join-Path $Folder 'plan.md'
$reviewPath = Join-Path $Folder 'review.md'
foreach ($path in @($planPath, $reviewPath)) {
  if (-not (Test-Path -LiteralPath $path)) { Stop-WithLines @("No file: $path") }
}
$planLines = Get-Content -LiteralPath $planPath
$reviewText = Get-Content -Raw -LiteralPath $reviewPath

$cycleLine = $planLines | Where-Object { $_ -match '^Cycle:\s*(\d+)\s*$' } | Select-Object -First 1
if (-not $cycleLine) { Stop-WithLines @('plan.md has no Cycle: line.') }
$cycle = [int]([regex]::Match($cycleLine, '^Cycle:\s*(\d+)').Groups[1].Value)
if (-not ($planLines | Where-Object { $_ -match "^### Cycle $cycle\s*$" })) {
  Stop-WithLines @("plan.md has no '### Cycle $cycle' section.")
}

$problems = [Collections.Generic.List[string]]::new()

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
foreach ($text in @(Get-CommitProblems $commits)) { $problems.Add($text) }

$changed = @(Invoke-Git @('diff', '--name-only', '--no-renames', 'HEAD')) + @(Invoke-Git @('ls-files', '--others', '--exclude-standard'))
$changed = @($changed | Where-Object { $_ } | ForEach-Object { $_ -replace '\\', '/' } | Select-Object -Unique)
$pathsByGroup = @{}
foreach ($group in $GroupOrder) { $pathsByGroup[$group] = [Collections.Generic.List[string]]::new() }
foreach ($path in $changed) { $pathsByGroup[(Get-CommitGroup $path)].Add($path) }

$mergeBase = @(Invoke-Git @('merge-base', 'HEAD', $MainBranch))[0]
$loggedSubjects = @(Invoke-Git @('log', '--format=%s', "$mergeBase..HEAD"))
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

foreach ($group in $GroupOrder) {
  $commit = $commits | Where-Object Group -eq $group | Select-Object -First 1
  if (-not $commit -or $isDone[$group] -or $pathsByGroup[$group].Count -eq 0) { continue }
  $listFile = Join-Path ([IO.Path]::GetTempPath()) "hf-commit-cycle-$([Guid]::NewGuid().ToString('N')).txt"
  $addFile = Join-Path ([IO.Path]::GetTempPath()) "hf-commit-cycle-$([Guid]::NewGuid().ToString('N')).txt"
  try {
    # A path that is gone from the work tree and the index (the old path of "git mv") has nothing to add: its deletion
    # is already staged, and "git commit --only" commits it.
    $toAdd = @($pathsByGroup[$group] | Where-Object {
        (Test-Path -LiteralPath (Join-Path $Repo $_)) -or @(Invoke-Git @('ls-files', '--', $_)).Count -gt 0 })
    [IO.File]::WriteAllLines($listFile, [string[]]$pathsByGroup[$group])
    [IO.File]::WriteAllLines($addFile, [string[]]$toAdd)
    if ($toAdd.Count -gt 0) { [void](Invoke-Git @('add', '-A', "--pathspec-from-file=$addFile")) }
    [void](Invoke-Git @('commit', '-q', '-m', $commit.Subject, '--only', "--pathspec-from-file=$listFile"))
  } finally {
    Remove-Item -LiteralPath $listFile, $addFile -Force -ErrorAction SilentlyContinue
  }
  Write-Host "$(@(Invoke-Git @('rev-parse', '--short', 'HEAD'))[0]) $($commit.Subject)"
}

if (@(Invoke-Git @('status', '--porcelain')).Count -gt 0) { Stop-WithLines @('the checkout is not clean after the commits.') }
