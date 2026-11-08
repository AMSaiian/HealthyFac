#Requires -Version 7
# check-plan.ps1 - checks the one thing about a cycle that a model cannot check reliably: that the sessions can find
# every part of the cycle, and that each step holds its own facts (.claude/commands/spec.md, Handoff).
# Usage: pwsh .claude/check-plan.ps1 <feature folder> [-Cycle <N>]    (default: the highest cycle of plan.md)
# Output: one line for each problem. Exit code 1 when there is one.
# Problems:
# - the cycle has no "Spec:" line;
# - a section name of the Spec line (in **bold**) is no heading of spec.md;
# - an AC of the Spec line has no "*   **AC<n>:**" line in spec.md, or no row in "## Acceptance Criteria Map";
# - a step names "spec.md" or "the spec" outside the Spec line (the step is not self-contained).
param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [int]$Cycle = 0
)

$ErrorActionPreference = 'Stop'
$planPath = Join-Path $Folder 'plan.md'
$specPath = Join-Path $Folder 'spec.md'
foreach ($path in @($planPath, $specPath)) {
  if (-not (Test-Path -LiteralPath $path)) { Write-Error "No file: $path"; exit 1 }
}
$planLines = Get-Content -LiteralPath $planPath
$specLines = Get-Content -LiteralPath $specPath

function Expand-AcNumbers([string] $text) {
  $numbers = [Collections.Generic.List[int]]::new()
  foreach ($match in [regex]::Matches($text, 'AC(\d+)[a-z]*(?:\s*[-–]\s*AC(\d+)[a-z]*)?')) {
    $first = [int]$match.Groups[1].Value
    $last = if ($match.Groups[2].Success) { [int]$match.Groups[2].Value } else { $first }
    foreach ($number in $first..$last) { $numbers.Add($number) }
  }
  return $numbers
}

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

$problems = [Collections.Generic.List[string]]::new()
$specLine = $section | Where-Object { $_ -match '^Spec:\s*\S' } | Select-Object -First 1

if (-not $specLine) {
  $problems.Add("Cycle ${Cycle}: no Spec: line.")
} else {
  $specHeadings = $specLines | ForEach-Object {
    $match = [regex]::Match($_, '^#{1,6}\s+(.+?)\s*$')
    if ($match.Success) { $match.Groups[1].Value }
  }
  foreach ($name in [regex]::Matches($specLine, '\*\*([^*]+)\*\*') | ForEach-Object { $_.Groups[1].Value.Trim() }) {
    if ($name -notin $specHeadings) { $problems.Add("Cycle ${Cycle}: Spec: section '$name' is no heading of spec.md.") }
  }

  $mapRows = [Collections.Generic.List[string]]::new()
  $isInMap = $false
  foreach ($line in $planLines) {
    if ($line -match '^## ') { $isInMap = $line -match '^## Acceptance Criteria Map' }
    elseif ($isInMap -and $line -match '^\|\s*(.+?)\s*\|') { $mapRows.Add($Matches[1]) }
  }
  $mapNumbers = $mapRows | ForEach-Object { Expand-AcNumbers $_ }

  foreach ($number in Expand-AcNumbers $specLine) {
    $hasLine = $specLines | Where-Object { $_ -match "^\*\s+\*\*AC${number}[a-z]*:\*\*" } | Select-Object -First 1
    if (-not $hasLine) { $problems.Add("Cycle ${Cycle}: AC$number has no '*   **AC${number}:**' line in spec.md.") }
    if ($number -notin $mapNumbers) { $problems.Add("Cycle ${Cycle}: AC$number has no row in the Acceptance Criteria Map.") }
  }
}

$currentStep = $null
$stepProblems = [Collections.Generic.HashSet[string]]::new()
foreach ($line in $section) {
  $stepMatch = [regex]::Match($line, '^- \[.\] Step (\d+)')
  if ($stepMatch.Success) { $currentStep = $stepMatch.Groups[1].Value }
  if ($null -ne $currentStep -and $line -match '(?i)spec\.md|\bthe spec\b' -and $stepProblems.Add($currentStep)) {
    $problems.Add("Cycle ${Cycle}: Step $currentStep names the spec. Copy the facts into the step.")
  }
}

$problems
if ($problems.Count -gt 0) { exit 1 }
