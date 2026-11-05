# check-review-routing.ps1 - prints the one allowed handoff target for a review.md, so the
# reviewer routes by a fixed rule (.claude/commands/review.md, Handoff).
# Usage: pwsh .claude/check-review-routing.ps1 <path to review.md>
# Output: one line "<target> <reason>", where target is "spec" or "executor".
# Exit code 1 when review.md lacks the Cycle line, the Round line, the verdict, or the summary table.
param([Parameter(Mandatory = $true)][string]$ReviewPath)

$NotConvergingRound = 4
$ReopenLimit = 2

if (-not (Test-Path $ReviewPath)) { Write-Error "No file: $ReviewPath"; exit 1 }
$text = Get-Content -Raw -Path $ReviewPath

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
$lastRows = $summaryRows | Select-Object -Last 3
$openSpec = ($lastRows | ForEach-Object { [int]$_.Groups[3].Value } | Measure-Object -Sum).Sum

$reopenCounts = [regex]::Matches($text, 'REOPENED\s*\((\d+)\)') | ForEach-Object { [int]$_.Groups[1].Value }
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
