# check-review-routing.ps1 - prints the one allowed handoff target for a review.md, so the
# reviewer routes by a fixed rule (.claude/agents/reviewer.md, Report).
# Usage: pwsh .claude/check-review-routing.ps1 <path to review.md> -Scope <cycle|branch>
# -Scope branch (the task "review branch"): line 4 of review.md must be "Scope: branch" (the review of the whole
# branch). -Scope cycle (every other task): no line of review.md may be "Scope: branch". A mismatch exits 1 with
# "review.md line 4 does not match the scope <scope>: write or remove 'Scope: branch'."
# Output: one line "<target> <reason>", where target is "spec", "executor", or "branch". An APPROVE goes to the executor
# ("executor approved (...)"): it commits the cycle (.claude/commit-cycle.ps1). With -Scope branch the lines are
# "branch approved (cycle C)" for APPROVE and "branch findings (cycle C)" for CHANGES_REQUESTED.
# The script counts the "### F<n> <SEVERITY> [<CODE|SPEC>]" headings of "## Open"; a summary table in review.md is
# ignored. Routing order for -Scope cycle: "executor approved" (APPROVE); "spec not-converging"; "executor code-fixes
# (cycle C, round R, open [SPEC] n)" when [CODE] and [SPEC] findings are open; "spec spec-findings" when only [SPEC]
# findings are open; "executor code-fixes (cycle C, round R)" when only [CODE] findings are open.
# Exit code 1 when review.md lacks the Cycle line, the Round line, the verdict, or the "## Open" section, and when:
# - a "### " heading in "## Open" does not have the form "### F<n> <BLOCKER|MAJOR|MINOR> [<CODE|SPEC>] <title>";
# - APPROVE has an open BLOCKER or MAJOR.
# Every check above also applies with -Scope branch.
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
$blocking = $counts['BLOCKER CODE'] + $counts['BLOCKER SPEC'] + $counts['MAJOR CODE'] + $counts['MAJOR SPEC']
if ($verdict.Groups[1].Value -eq 'APPROVE' -and $blocking -gt 0) {
  $errors.Add("Verdict: APPROVE with $blocking open BLOCKER or MAJOR findings.")
}
if ($errors.Count -gt 0) { $errors | ForEach-Object { Write-Error $_ -ErrorAction Continue }; exit 1 }
$openSpec = $counts['BLOCKER SPEC'] + $counts['MAJOR SPEC'] + $counts['MINOR SPEC']
$openCode = $counts['BLOCKER CODE'] + $counts['MAJOR CODE'] + $counts['MINOR CODE']

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
} elseif ($openSpec -gt 0 -and $openCode -gt 0) {
  "executor code-fixes (cycle $c, round $r, open [SPEC] $openSpec)"
} elseif ($openSpec -gt 0) {
  "spec spec-findings (cycle $c, round $r, open [SPEC] $openSpec)"
} else {
  "executor code-fixes (cycle $c, round $r)"
}
