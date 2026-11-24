#Requires -Version 7
# check-handoff.ps1 - the pre-review check of the executor: it finds the mechanical findings before the review
# (.claude/agents/executor.md, Handoff).
# Usage: pwsh .claude/check-handoff.ps1 <feature folder> [-Repo <path>] [-Base <ref>] [-Cycle <N>]
#        [-MainBranch <name>] [-FormatScript <path>]
# Defaults: -Repo is the checkout that holds this script file (not the current folder); -Base is the merge base of
# HEAD and -MainBranch (develop), so a main branch that moved adds no files of other features; -Cycle is the Cycle:
# line of plan.md (the highest cycle when plan.md has none); -FormatScript is format-changed.ps1 next to this file.
# Output: one line for each problem. Exit code 1 when there is one.
# It runs the format script on the changed files first, so it can change files: each changed file is a problem. After
# run-area.ps1, the format script skips the files that it formatted at the same tree.
# Problems:
# - a step of the cycle that is not [x] or [~];
# - a step whose text says TDD, Red, or "Proof required", with no Red: or Proof: line in its own block (under the step);
# - a Deviations entry of the cycle that holds a Red: or Proof: line, or that only logs a run ("Step N: done");
# - a changed or untracked file whose path or file name is in no "Files:" line and no "#### Deviations" entry of any
#   cycle (the base holds the earlier cycles of the branch too);
# - a file that the format script changed, and each note of a skipped cleanup;
# - a line of a changed C# file (outside Migrations/) that is longer than max_line_length of .editorconfig;
# - a failing script test, when the change touches the flow scripts, the rules, the hooks, or the verify stack;
# - a test area (tree-id.ps1 -Area backend|frontend) that differs from the base and has no "Full run <area>: tree <id>"
#   line with its current tree in plan.md;
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
$Areas = @('backend', 'frontend')
# The test script of each group of tool files (paths relative to the repo; the first pattern that matches a changed
# file runs the script once).
$ScriptTests = [ordered]@{
  '.claude/tests/rules.tests.ps1'         = '^(\.claude/rules/|CLAUDE\.md$)'
  '.claude/hooks/tests/hook-scope.tests.ps1' = '^\.claude/hooks/'
  '.claude/tests/flow-scripts.tests.ps1'  = '^\.claude/[^/]+\.ps1$|^\.claude/tests/flow-scripts'
  'scripts/tests/verify-slots.tests.ps1'  = '^scripts/(verify-|tests/verify-)'
  'scripts/tests/agent-chrome.tests.ps1'  = '^scripts/(tests/)?agent-chrome'
}

$planPath = Join-Path $Folder 'plan.md'
if (-not (Test-Path -LiteralPath $planPath)) { Write-Error "No file: $planPath"; exit 1 }
$planLines = Get-Content -LiteralPath $planPath

if (-not $Base) {
  $Base = (git -C $Repo merge-base HEAD $MainBranch 2>$null)
  if ($LASTEXITCODE -ne 0 -or -not $Base) { Write-Error "No merge base of HEAD and $MainBranch in $Repo. Pass -Base <ref>."; exit 1 }
  $Base = $Base.Trim()
}

$planCycle = Get-PlanCycle $planLines $Cycle
$Cycle = $planCycle.Number
$section = $planCycle.Section
$steps = @(Get-CycleSteps $section)
$deviationLines = @(Get-CycleDeviations $section)

$problems = [Collections.Generic.List[string]]::new()

foreach ($step in $steps) {
  if ($step.Mark -notin @('x', '~')) { $problems.Add("Step $($step.Number) is not done ([$($step.Mark)]).") }
}

# A Red: or Proof: line goes under its step in the cycle section, so each step is checked on its own text.
foreach ($step in $steps) {
  $text = $step.Lines -join "`n"
  $hasProofLine = $text -cmatch '(?m)\b(Red|Proof)(\s*\([^)]*\))?:'
  if (($text -cmatch '\b(TDD|Red)\b' -or $text -cmatch '\bProof required\b') -and -not $hasProofLine) {
    $problems.Add("Step $($step.Number) names TDD, Red, or Proof required, but its block has no Red: or Proof: line.")
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
    $isWaiting = $line -match '^- \[!\].*\bWAITING:'
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

foreach ($test in $ScriptTests.Keys) {
  $testPath = Join-Path $Repo $test
  if (-not ($changed -match $ScriptTests[$test]) -or -not (Test-Path -LiteralPath $testPath)) { continue }
  $output = @(& pwsh -NoProfile -File $testPath 2>&1 | ForEach-Object { "$_" })
  if ($LASTEXITCODE -ne 0) {
    $failed = @($output | Where-Object { $_ -match '^\s*FAIL ' } | ForEach-Object { $_.Trim() } | Select-Object -First 5)
    $problems.Add("The script test $test fails: $(if ($failed) { $failed -join '; ' } else { $output | Select-Object -Last 1 })")
  }
}

$areaRuns = [regex]::Matches(($planLines -join "`n"), 'Full run (backend|frontend):\s*tree\s+([0-9a-f]{40})')
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
