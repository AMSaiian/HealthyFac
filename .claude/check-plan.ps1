#Requires -Version 7
# check-plan.ps1 - checks the one thing about a cycle that a model cannot check reliably: that the sessions can find
# every part of the cycle, and that each step holds its own facts (.claude/commands/spec.md, Handoff).
# Usage: pwsh .claude/check-plan.ps1 <feature folder> [-Cycle <N>]    (default: the highest cycle of plan.md)
# Output: one line for each problem. Exit code 1 when there is one.
# Problems:
# - the cycle has no "Spec:" line;
# - a section name of the Spec line (in **bold**) is no heading of spec.md;
# - an AC of the Spec line has no "*   **AC<n>:**" line in spec.md, or no row in "## Acceptance Criteria Map";
# - a step names "spec.md" or "the spec" outside the Spec line (the step is not self-contained);
# - a step whose Files: line names a test file, without "TDD", "Proof required", or "No proof: <reason>" in its block;
# - the cycle has no "#### Deviations" heading (the cycle keeps its own Deviations, so one range read holds them);
# - an AC number with more than one "*   **AC<n>:**" line in spec.md (a number is never reused).
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

. (Join-Path $PSScriptRoot 'plan-sections.ps1')
$planCycle = Get-PlanCycle $planLines $Cycle
$Cycle = $planCycle.Number
$section = $planCycle.Section

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

$steps = @(Get-CycleSteps $section)

$TestFilePattern = '(?i)(^|[/`\s])tests?/|\.spec\.ts\b|Tests?\.cs\b|\.tests\.ps1\b'
foreach ($step in $steps) {
  $text = $step.Lines -join "`n"
  if ($text -match '(?i)spec\.md|\bthe spec\b') {
    $problems.Add("Cycle ${Cycle}: Step $($step.Number) names the spec. Copy the facts into the step.")
  }
  $filesText = ($step.Lines | Where-Object { $_ -match 'Files:' }) -join "`n"
  if ($filesText -match $TestFilePattern -and $text -cnotmatch '\bTDD\b|\bProof required\b|\bNo proof:') {
    $problems.Add("Cycle ${Cycle}: Step $($step.Number) changes a test file, but says neither TDD, Proof required, nor No proof: <reason>.")
  }
}

if (-not ($section | Where-Object { $_ -match '^#### Deviations\s*$' })) {
  $problems.Add("Cycle ${Cycle}: no '#### Deviations' heading. End the cycle section with it.")
}

$acLines = $specLines | ForEach-Object {
  $match = [regex]::Match($_, '^\*\s+\*\*AC(\d+[a-z]*):\*\*')
  if ($match.Success) { $match.Groups[1].Value }
}
foreach ($group in $acLines | Group-Object | Where-Object Count -gt 1) {
  $problems.Add("spec.md has $($group.Count) lines for AC$($group.Name). Never reuse an AC number.")
}

$problems
if ($problems.Count -gt 0) { exit 1 }
