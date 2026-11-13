# check-review-routing.ps1 - prints the one allowed handoff target for a review.md, so the
# reviewer routes by a fixed rule (.claude/commands/review.md, Handoff).
# Usage: pwsh .claude/check-review-routing.ps1 <path to review.md>
# Output: one line "<target> <reason>", where target is "spec" or "executor".
# Exit code 1 when review.md lacks the Cycle line, the Round line, the verdict, or the summary table.
# A review.md with a "## Open" section (the current layout of review.md) is also checked:
# - every "### " heading in "## Open" has the form "### F<n> <BLOCKER|MAJOR|MINOR> [<CODE|SPEC>] <title>";
# - the summary table equals the count of the open findings by severity and target;
# - APPROVE has no open BLOCKER or MAJOR;
# - REOPENED counts only inside "## Open", so a closed or accepted finding never routes.
# An older review.md (no "## Open") keeps the old rules: checkpoint sections are dropped, and the last table counts.
param([Parameter(Mandatory = $true)][string]$ReviewPath)

$NotConvergingRound = 4
$ReopenLimit = 2
$Severities = @('BLOCKER', 'MAJOR', 'MINOR')

if (-not (Test-Path $ReviewPath)) { Write-Error "No file: $ReviewPath"; exit 1 }
$text = Get-Content -Raw -Path $ReviewPath
$openSection = [regex]::Match($text, '(?ms)^## Open\s*$(.*?)(?=^## |\z)')
if (-not $openSection.Success) {
  # A "## Checkpoint <x>" section of the old layout holds findings of a checkpoint review. It never routes.
  $text = [regex]::Replace($text, '(?ms)^## Checkpoint\b.*?(?=^## |\z)', '')
}

$cycle = [regex]::Match($text, '(?m)^Cycle:\s*(\d+)\s*$')
$round = [regex]::Match($text, '(?m)^Round:\s*(\d+)\s*$')
$verdict = [regex]::Match($text, '(?m)^Verdict:\s*(APPROVE|CHANGES_REQUESTED)\s*$')
if (-not $cycle.Success -or -not $round.Success -or -not $verdict.Success) {
  Write-Error 'review.md needs the lines "Cycle: C", "Round: R", and "Verdict: APPROVE|CHANGES_REQUESTED".'
  exit 1
}

$summaryRows = [regex]::Matches($text, '(?m)^\|\s*(BLOCKER|MAJOR|MINOR)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*$')
if ($summaryRows.Count -lt 3) {
  Write-Error 'review.md needs the summary table "| Severity | [CODE] | [SPEC] |" with the BLOCKER, MAJOR, and MINOR rows.'
  exit 1
}
$lastRows = @($summaryRows | Select-Object -Last 3)
$openSpec = ($lastRows | ForEach-Object { [int]$_.Groups[3].Value } | Measure-Object -Sum).Sum
$reopenScope = $text

if ($openSection.Success) {
  $openText = $openSection.Groups[1].Value
  $reopenScope = $openText
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
  foreach ($row in $lastRows) {
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
}

$reopenCounts = [regex]::Matches($reopenScope, 'REOPENED\s*\((\d+)\)') | ForEach-Object { [int]$_.Groups[1].Value }
$maxReopened = ($reopenCounts | Measure-Object -Maximum).Maximum
if ($null -eq $maxReopened) { $maxReopened = 0 }

$r = [int]$round.Groups[1].Value
$c = [int]$cycle.Groups[1].Value

if ($verdict.Groups[1].Value -eq 'APPROVE') {
  "spec approved (cycle $c, round $r)"
} elseif ($maxReopened -ge $ReopenLimit -or $r -ge $NotConvergingRound) {
  "spec not-converging (cycle $c, round $r, max REOPENED $maxReopened)"
} elseif ($openSpec -gt 0) {
  "spec spec-findings (cycle $c, round $r, open [SPEC] $openSpec)"
} else {
  "executor code-fixes (cycle $c, round $r)"
}
