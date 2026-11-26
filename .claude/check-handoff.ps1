#Requires -Version 7
# check-handoff.ps1 - the pre-review check of the executor: it finds the mechanical findings before the review
# (.claude/agents/executor.md, Handoff).
# Usage: pwsh .claude/check-handoff.ps1 <feature folder> [-Repo <path>] [-Base <ref>] [-Cycle <N>]
#        [-MainBranch <name>] [-FormatScript <path>]
# Defaults: -Repo is the checkout that holds this script file (not the current folder); -Base is the "Base:" SHA of the
# plan header, else the merge base of HEAD and origin/-MainBranch (develop; the local branch when origin has none, Get-DefaultBase), so a main branch that
# moved adds no files of other features; -Cycle is the Cycle:
# line of plan.md (the highest cycle when plan.md has none); -FormatScript is format-changed.ps1 next to this file.
# Output: one line for each problem. Exit code 1 when there is one.
# Phase 1, the text checks in this order: the step ticks, the Red: and Proof: lines, the browser flow lines and the
# WAITING marks, the Deviations entries, the Files: lines. When phase 1 has a problem, the script prints the problems,
# then "Text checks failed: the format pass and the run lines were not checked.", and exits 1.
# Phase 2 runs the format script on the changed files, so it can change files: each changed file is a problem. After
# run-area.ps1, the format script skips the files that it formatted at the same tree. Then it checks the long lines and
# the Full run lines. It runs no script test suite: run-area.ps1 -Area tooling does (CLAUDE.md, Build and Test).
# Problems:
# - a step of the cycle that is not [x] or [~] (a browser check step marked [!] with WAITING: and retried: passes);
# - a step whose text says TDD, Red, or "Proof required", with no Red: or Proof: line in its own block (under the step);
# - a TDD step (TDD on its first line) whose block has a line that starts with Proof: but none that starts with Red: (a
#   "Proof required" step keeps the Red: or Proof: rule);
# - a Deviations entry of the cycle that holds a Red: or Proof: line, or that only logs a run ("Step N: done");
# - a changed or untracked file whose path or file name is in no "Files:" line and no "#### Deviations" entry of any
#   cycle (the base holds the earlier cycles of the branch too);
# - a file that the format script changed, and each note of a skipped cleanup;
# - a line of a changed C# file (outside Migrations/) that is longer than max_line_length of .editorconfig;
# - a test area (tree-id.ps1 -Area backend|frontend|tooling) that differs from the base and has no "Full run <area>:
#   tree <id>" line with its current tree in plan.md (also for tooling, in every cycle);
# - a browser flow line of a step (.claude/browser-checks.md, Flow Lines): a "pass" line without "seen:", or with a
#   caveat (a timeout, "not checked", a minimized window, a faked size, a script drag, a stubbed function, a DOM-only
#   measurement; the text of a "retried:" part does not count); a "fail" line or a WAITING step that names a transient
#   error (a timeout, an unresponsive renderer, a slot that did not start, a flaky test) without "retried:".
# The Deviations of the cycle are the bullets under "#### Deviations" in the cycle section.
param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$Base,
  [int]$Cycle = 0,
  [string]$MainBranch = 'develop',
  [string]$FormatScript = (Join-Path $PSScriptRoot 'format-changed.ps1')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'plan-sections.ps1')
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$Areas = @('backend', 'frontend', 'tooling')
$planPath = Join-Path $Folder 'plan.md'
if (-not (Test-Path -LiteralPath $planPath)) { Write-Error "No file: $planPath"; exit 1 }
$planLines = Get-Content -LiteralPath $planPath

if (-not $Base) {
  $Base = Get-FeatureBase -Repo $Repo -PlanLines $planLines -MainBranch $MainBranch
  if (-not $Base) { Write-Error "No merge base of HEAD and $MainBranch in $Repo. Pass -Base <ref>."; exit 1 }
}

$planCycle = Get-PlanCycle $planLines $Cycle
$Cycle = $planCycle.Number
$section = $planCycle.Section
$steps = @(Get-CycleSteps $section)
$deviationLines = @(Get-CycleDeviations $section)

$problems = [Collections.Generic.List[string]]::new()

# A browser check step is a step whose first line says "browser check". Marked "[!] ... WAITING: <error>; retried:
# <what>", it passes (the executor retries it at the start of each run, and the last cycle commits only after its flows
# pass, commit-cycle.ps1).
foreach ($step in $steps) {
  if ($step.Mark -in @('x', '~')) { continue }
  $first = $step.Lines[0]
  $isWaitingBrowserCheck = (Get-BrowserCheckMark $step) -eq 'waiting' -and $first -match '(?i)\bretried:'
  if (-not $isWaitingBrowserCheck) { $problems.Add("Step $($step.Number) is not done ([$($step.Mark)]).") }
}

# A Red: or Proof: line goes under its step in the cycle section, so each step is checked on its own text.
foreach ($step in $steps) {
  $text = $step.Lines -join "`n"
  $hasProofLine = $text -cmatch '(?m)\b(Red|Proof)(\s*\([^)]*\))?:'
  if (($text -cmatch '\b(TDD|Red)\b' -or $text -cmatch '\bProof required\b') -and -not $hasProofLine) {
    $problems.Add("Step $($step.Number) names TDD, Red, or Proof required, but its block has no Red: or Proof: line.")
  } elseif ($step.Lines[0] -cmatch '\bTDD\b' -and $text -cmatch '(?m)^\s*(?:[*-]\s+)?Proof(\s*\([^)]*\))?:' -and
    $text -cnotmatch '(?m)^\s*(?:[*-]\s+)?Red(\s*\([^)]*\))?:') {
    $problems.Add("Step $($step.Number) is TDD, but its block has only Proof: lines: a TDD step needs the Red: line of its red run.")
  }
}

$FlowLinePattern = '(?i)^\s*(?:[*-]\s+)?(?:Flow [^:]{1,60}:\s*)?(?:round\s+\d+:\s*)?(pass|fail)\s*[:;]'
$CaveatPattern = '(?i)\btimed?[ -]?out\b|\bnot checked\b|minimi[sz]ed|\b(root|css)\s+zoom\b|\bzoom layout\b|style\.zoom|' +
  '\bstubbed\b|dispatchEvent|\bsynthetic\b|\bscripted\b|sent as (mouse|pointer) events|measured in the DOM|\bDOM only\b|\bemulated\b'
$TransientPattern = '(?i)\btimed?[ -]?out\b|\btimeout\b|unresponsive|not respond|did not start|unhealthy|\bflak'
foreach ($step in $steps) {
  foreach ($line in $step.Lines) {
    $start = $line.Trim().Substring(0, [Math]::Min(70, $line.Trim().Length))
    $flow = [regex]::Match($line, $FlowLinePattern)
    $isWaiting = $line -eq $step.Lines[0] -and (Get-BrowserCheckMark $step) -eq 'waiting'
    if (-not $flow.Success -and -not $isWaiting) { continue }
    $hasRetry = $line -match '(?i)\bretried:'
    if ($flow.Success -and $flow.Groups[1].Value -eq 'pass') {
      $withoutRetry = $line -replace '(?i)\bretried:[^;]*', ''
      if ($withoutRetry -match $CaveatPattern) {
        $problems.Add("Step $($step.Number): a pass line has a caveat ('$start'). A flow that did not run as written is no pass (.claude/browser-checks.md, Flow Lines).")
      } elseif ($line -notmatch '(?i)\bseen:') {
        $problems.Add("Step $($step.Number): a pass line has no 'seen: <size> <theme>' ('$start').")
      }
    } elseif ($line -match $TransientPattern -and -not $hasRetry) {
      $problems.Add("Step $($step.Number): a transient error without 'retried:' ('$start'). Fix it and run the same command once more (.claude/browser-checks.md, Errors).")
    }
  }
}

foreach ($line in $deviationLines) {
  $start = $line.Trim().Substring(0, [Math]::Min(70, $line.Trim().Length))
  if ($line -cmatch '\b(Red|Proof)(\s*\([^)]*\))?:') {
    $problems.Add("A Deviations entry of cycle $Cycle holds a Red: or Proof: line ('$start'). Move the line under its step.")
  } elseif ($line -match '^(\*|-)\s+Steps?\s+[\d,\s–-]+:\s*done\b') {
    $problems.Add("A Deviations entry of cycle $Cycle only logs a run ('$start'). Remove it: the tick shows that the step is done.")
  }
}

# The default base holds the earlier cycles of the branch too, so a file that any cycle names (a Files: line or a
# "#### Deviations" entry) is known.
$planFilesText = ($planLines | Where-Object { $_ -match 'Files:' }) -join "`n"
$planDeviationLines = [Collections.Generic.List[string]]::new()
$isInAnyDeviations = $false
foreach ($line in $planLines) {
  if ($line -match '^#{1,4} ') { $isInAnyDeviations = $line -match '^#### Deviations\s*$'; continue }
  if ($isInAnyDeviations) { $planDeviationLines.Add($line) }
}
$knownText = ($planFilesText + "`n" + ($planDeviationLines -join "`n")).Replace('\', '/')
$changed = @(@(git -C $Repo diff --name-only $Base) + @(git -C $Repo ls-files --others --exclude-standard) |
    Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
foreach ($path in $changed) {
  $name = Split-Path $path -Leaf
  if (-not $knownText.Contains($path, [StringComparison]::OrdinalIgnoreCase) -and
    -not $knownText.Contains($name, [StringComparison]::OrdinalIgnoreCase)) {
    $problems.Add("File $path is in no Files: line and no Deviations entry of plan.md.")
  }
}

if ($problems.Count -gt 0) {
  $problems
  'Text checks failed: the format pass and the run lines were not checked.'
  exit 1
}

$existing = @($changed | Where-Object { Test-Path -LiteralPath (Join-Path $Repo $_) -PathType Leaf })
if ($existing.Count -gt 0 -and $FormatScript -and (Test-Path -LiteralPath $FormatScript)) {
  $before = @{}
  foreach ($path in $existing) { $before[$path] = (Get-FileHash -LiteralPath (Join-Path $Repo $path)).Hash }
  $notes = @(& $FormatScript -Repo $Repo -Files $existing)
  foreach ($note in $notes) { $problems.Add("Formatter: $note") }
  foreach ($path in $existing) {
    if ((Get-FileHash -LiteralPath (Join-Path $Repo $path)).Hash -ne $before[$path]) {
      $problems.Add("The formatter changed $path. Run the tests of its area again and write a new Full run line.")
    }
  }
}

foreach ($line in @(Get-LongLines -Repo $Repo -Files $existing)) {
  $problems.Add("$($line.File):$($line.Line) has $($line.Length) characters; max_line_length is $($line.Limit). Split the line.")
}

$areaRuns = [regex]::Matches(($planLines -join "`n"), 'Full run (backend|frontend|tooling):\s*tree\s+([0-9a-f]{40})')
foreach ($area in $Areas) {
  $current = Get-TreeId -Repo $Repo -Area $area
  $atBase = Get-TreeId -Repo $Repo -Area $area -Ref $Base
  if ($current -eq $atBase) { continue }
  $tested = @($areaRuns | Where-Object { $_.Groups[1].Value -eq $area } | ForEach-Object { $_.Groups[2].Value })
  if ($current -in $tested) { continue }
  if ($tested.Count -eq 0) {
    $problems.Add("Cycle ${Cycle}: the $area area changed since the base, but plan.md has no 'Full run ${area}: tree <id>' line.")
  } else {
    $problems.Add("The last Full run ${area}: tree $($tested[-1]) is not the current $area tree $current. Run the $area full suite again.")
  }
}

$problems
if ($problems.Count -gt 0) { exit 1 }
