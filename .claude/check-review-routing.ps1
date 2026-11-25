# check-review-routing.ps1 - prints the one allowed handoff target for a review.md, so the
# reviewer routes by a fixed rule (.claude/agents/reviewer.md, Report).
# Usage: pwsh .claude/check-review-routing.ps1 <path to review.md> -Scope <cycle|branch>
# -Scope branch (the task "review branch"): line 4 of review.md must be "Scope: branch". -Scope cycle (every other task):
# no line of review.md may be "Scope: branch". A mismatch exits 1 with "review.md line 4 does not match the scope <scope>:
# write or remove 'Scope: branch'."
# Output: one line "<target> <reason>", where target is "spec" or "executor". An APPROVE goes to the executor
# ("executor approved (...)"): it commits the cycle (.claude/commit-cycle.ps1).
# Exit code 1 when review.md lacks the Cycle line, the Round line, the verdict, the "## Open" section, or the summary
# table, and when:
# - a "### " heading in "## Open" does not have the form "### F<n> <BLOCKER|MAJOR|MINOR> [<CODE|SPEC>] <title>";
# - the summary table differs from the count of the open findings by severity and target;
# - APPROVE has an open BLOCKER or MAJOR.
# With -Scope branch (the file must hold the line "Scope: branch" at line 4, the review of the whole branch), the
# script routes "branch approved (cycle C)" for APPROVE and "branch findings (cycle C)" for CHANGES_REQUESTED. With
# -Scope cycle, that line fails the check. Every check above still applies.
# REOPENED counts only inside "## Open", so a closed or accepted finding never routes.
param(
  [Parameter(Mandatory = $true)][string]$ReviewPath,
  [Parameter(Mandatory = $true)][ValidateSet('cycle', 'branch')][string]$Scope
)

$NotConvergingRound = 4
$ReopenLimit = 2
$Severities = @('BLOCKER', 'MAJOR', 'MINOR')

if (-not (Test-Path $ReviewPath)) { Write-Error "No file: $ReviewPath"; exit 1 }
$text = Get-Content -Raw -Path $ReviewPath

$hasBranchLine = [regex]::IsMatch($text, '(?m)^Scope:\s*branch\s*$')
$isLineFourBranch = @($text -split '\r?\n')[3] -match '^Scope:\s*branch\s*$'
if (($Scope -eq 'branch' -and -not $isLineFourBranch) -or ($Scope -eq 'cycle' -and $hasBranchLine)) {
  Write-Error "review.md line 4 does not match the scope ${Scope}: write or remove 'Scope: branch'."
  exit 1
}

$cycle = [regex]::Match($text, '(?m)^Cycle:\s*(\d+)\s*$')
$round = [regex]::Match($text, '(?m)^Round:\s*(\d+)\s*$')
$verdict = [regex]::Match($text, '(?m)^Verdict:\s*(APPROVE|CHANGES_REQUESTED)\s*$')
if (-not $cycle.Success -or -not $round.Success -or -not $verdict.Success) {
  Write-Error 'review.md needs the lines "Cycle: C", "Round: R", and "Verdict: APPROVE|CHANGES_REQUESTED".'
  exit 1
}

$openSection = [regex]::Match($text, '(?ms)^## Open\s*$(.*?)(?=^## |\z)')
if (-not $openSection.Success) {
  Write-Error 'review.md has no "## Open" section.'
  exit 1
}

$summaryRows = @([regex]::Matches($text, '(?m)^\|\s*(BLOCKER|MAJOR|MINOR)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*$'))
if ($summaryRows.Count -ne 3) {
  Write-Error 'review.md needs one summary table "| Severity | [CODE] | [SPEC] |" with the BLOCKER, MAJOR, and MINOR rows.'
  exit 1
}

$openText = $openSection.Groups[1].Value
$errors = [Collections.Generic.List[string]]::new()
$counts = @{}
foreach ($severity in $Severities) { $counts["$severity CODE"] = 0; $counts["$severity SPEC"] = 0 }
foreach ($heading in [regex]::Matches($openText, '(?m)^### (.*)$')) {
  $finding = [regex]::Match($heading.Groups[1].Value, '^F\d+ (BLOCKER|MAJOR|MINOR) \[(CODE|SPEC)\] \S')
  if (-not $finding.Success) {
    $errors.Add("The heading '### $($heading.Groups[1].Value.Trim())' in ## Open is not '### F<n> <SEVERITY> [<TARGET>] <title>'.")
    continue
  }
  $counts["$($finding.Groups[1].Value) $($finding.Groups[2].Value)"]++
}
foreach ($row in $summaryRows) {
  $severity = $row.Groups[1].Value
  if ([int]$row.Groups[2].Value -ne $counts["$severity CODE"] -or [int]$row.Groups[3].Value -ne $counts["$severity SPEC"]) {
    $errors.Add("The summary row $severity says $($row.Groups[2].Value) [CODE] and $($row.Groups[3].Value) [SPEC], " +
      "but ## Open has $($counts["$severity CODE"]) and $($counts["$severity SPEC"]).")
  }
}
$blocking = $counts['BLOCKER CODE'] + $counts['BLOCKER SPEC'] + $counts['MAJOR CODE'] + $counts['MAJOR SPEC']
if ($verdict.Groups[1].Value -eq 'APPROVE' -and $blocking -gt 0) {
  $errors.Add("Verdict: APPROVE with $blocking open BLOCKER or MAJOR findings.")
}
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ -ErrorAction Continue }; exit 1 }
$openSpec = $counts['BLOCKER SPEC'] + $counts['MAJOR SPEC'] + $counts['MINOR SPEC']

$reopenCounts = [regex]::Matches($openText, 'REOPENED\s*\((\d+)\)') | ForEach-Object { [int]$_.Groups[1].Value }
$maxReopened = ($reopenCounts | Measure-Object -Maximum).Maximum
if ($null -eq $maxReopened) { $maxReopened = 0 }

$isBranchReview = $Scope -eq 'branch'
$r = [int]$round.Groups[1].Value
$c = [int]$cycle.Groups[1].Value

if ($isBranchReview) {
  if ($verdict.Groups[1].Value -eq 'APPROVE') { "branch approved (cycle $c)" } else { "branch findings (cycle $c)" }
} elseif ($verdict.Groups[1].Value -eq 'APPROVE') {
  "executor approved (cycle $c, round $r)"
} elseif ($maxReopened -ge $ReopenLimit -or $r -ge $NotConvergingRound) {
  "spec not-converging (cycle $c, round $r, max REOPENED $maxReopened)"
} elseif ($openSpec -gt 0) {
  "spec spec-findings (cycle $c, round $r, open [SPEC] $openSpec)"
} else {
  "executor code-fixes (cycle $c, round $r)"
}
