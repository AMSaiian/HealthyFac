#Requires -Version 7
# check-handoff.ps1 - the pre-review check of the executor: it finds the mechanical findings before the review
# (.claude/commands/implement.md, Handoff).
# Usage: pwsh .claude/check-handoff.ps1 <feature folder> -Repo <path> -Base <ref> [-Cycle <N>]
#        (default cycle: the highest cycle of plan.md)
# Output: one line for each problem. Exit code 1 when there is one.
# Problems:
# - a step of the cycle that is not [x] or [~];
# - a step whose text says TDD or Red, with no Red: or Proof: line in its own block (under the step);
# - a changed or untracked file whose path or file name is in no "Files:" line of the cycle and no Deviations entry;
# - no "Full run: tree <id>" line, or its tree ID differs from the current output of tree-id.ps1.
param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [Parameter(Mandatory = $true)][string]$Repo,
  [Parameter(Mandatory = $true)][string]$Base,
  [int]$Cycle = 0
)

$ErrorActionPreference = 'Stop'
$planPath = Join-Path $Folder 'plan.md'
if (-not (Test-Path -LiteralPath $planPath)) { Write-Error "No file: $planPath"; exit 1 }
$planLines = Get-Content -LiteralPath $planPath

$cycleHeadings = for ($i = 0; $i -lt $planLines.Count; $i++) {
  $match = [regex]::Match($planLines[$i], '^### Cycle (\d+)\s*$')
  if ($match.Success) { [pscustomobject]@{ Number = [int]$match.Groups[1].Value; Index = $i } }
}
if (-not $cycleHeadings) { Write-Error 'plan.md has no "### Cycle N" section.'; exit 1 }
if ($Cycle -eq 0) { $Cycle = ($cycleHeadings | Measure-Object Number -Maximum).Maximum }
$heading = $cycleHeadings | Where-Object Number -eq $Cycle | Select-Object -First 1
if (-not $heading) { Write-Error "plan.md has no section for cycle $Cycle."; exit 1 }

$end = $planLines.Count
for ($i = $heading.Index + 1; $i -lt $planLines.Count; $i++) {
  if ($planLines[$i] -match '^#{1,3} ') { $end = $i; break }
}
$section = $planLines[($heading.Index + 1)..($end - 1)]

$steps = [Collections.Generic.List[object]]::new()
foreach ($line in $section) {
  $stepMatch = [regex]::Match($line, '^- \[(.)\] Step (\d+)')
  if ($stepMatch.Success) {
    $steps.Add([pscustomobject]@{ Mark = $stepMatch.Groups[1].Value; Number = $stepMatch.Groups[2].Value; Text = [Text.StringBuilder]::new($line) })
  } elseif ($steps.Count -gt 0) {
    [void]$steps[$steps.Count - 1].Text.AppendLine($line)
  }
}
$stepNumbers = $steps | ForEach-Object Number

# The Deviations entries of the cycle: a bullet that starts with one of its steps ("Step N:" or "Steps A-B:"), or
# that names the cycle in its first 80 characters.
$deviationLines = [Collections.Generic.List[string]]::new()
$isInDeviations = $false
foreach ($line in $planLines) {
  if ($line -match '^## ') { $isInDeviations = $line -match '^## Deviations'; continue }
  if (-not $isInDeviations -or $line -notmatch '^(\*|-)\s+') { continue }
  $start = $line.Substring(0, [Math]::Min(80, $line.Length))
  $range = [regex]::Match($start, '^(?:\*|-)\s+Steps?\s+(\d+)(?:\s*[-–]\s*(\d+))?')
  $isStepEntry = $false
  if ($range.Success) {
    $first = [int]$range.Groups[1].Value
    $last = if ($range.Groups[2].Success) { [int]$range.Groups[2].Value } else { $first }
    $isStepEntry = [bool]($stepNumbers | Where-Object { [int]$_ -ge $first -and [int]$_ -le $last })
  }
  if ($isStepEntry -or $start -match "(?i)\bcycle $Cycle\b") { $deviationLines.Add($line) }
}
$deviationText = $deviationLines -join "`n"
$sectionText = $section -join "`n"
$evidenceText = $sectionText + "`n" + $deviationText

$problems = [Collections.Generic.List[string]]::new()

foreach ($step in $steps) {
  if ($step.Mark -notin @('x', '~')) { $problems.Add("Step $($step.Number) is not done ([$($step.Mark)]).") }
}

# A Red: or Proof: line goes under its step in the cycle section, so each step is checked on its own text.
foreach ($step in $steps) {
  $text = $step.Text.ToString()
  $hasProofLine = $text -match '(?m)\b(Red|Proof)(\s*\([^)]*\))?:'
  if ($text -cmatch '\b(TDD|Red)\b' -and -not $hasProofLine) {
    $problems.Add("Step $($step.Number) names TDD or Red, but its block has no Red: or Proof: line.")
  }
}

$filesText = ($section | Where-Object { $_ -match 'Files:' }) -join "`n"
$knownText = ($filesText + "`n" + $deviationText).Replace('\', '/')
$changed = @(git -C $Repo diff --name-only $Base) + @(git -C $Repo ls-files --others --exclude-standard) |
  Where-Object { $_ } | Sort-Object -Unique
foreach ($path in $changed) {
  $normalized = $path.Replace('\', '/')
  $name = Split-Path $normalized -Leaf
  if (-not $knownText.Contains($normalized, [StringComparison]::OrdinalIgnoreCase) -and
    -not $knownText.Contains($name, [StringComparison]::OrdinalIgnoreCase)) {
    $problems.Add("File $normalized is in no Files: line and no Deviations entry of cycle $Cycle.")
  }
}

$fullRuns = [regex]::Matches($evidenceText, 'Full run:\s*tree\s+([0-9a-f]{40})')
if ($fullRuns.Count -eq 0) {
  $problems.Add("Cycle ${Cycle}: no 'Full run: tree <id>' line.")
} else {
  $current = (& (Join-Path $PSScriptRoot 'tree-id.ps1') -Repo $Repo)
  $recorded = $fullRuns[$fullRuns.Count - 1].Groups[1].Value
  if ($recorded -ne $current) { $problems.Add("The Full run: tree $recorded is not the current tree $current. Run the full suite again.") }
}

$problems
if ($problems.Count -gt 0) { exit 1 }
