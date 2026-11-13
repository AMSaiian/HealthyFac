# plan-sections.ps1 - dot-sourced by check-plan.ps1 and check-handoff.ps1. It reads one cycle of a plan.md, so both
# checks agree on what a cycle, a step block, and the Deviations of a cycle are.

# The lines of "### Cycle <N>" up to the next heading of level 1 to 3. -Cycle 0 means the highest cycle.
# It writes an error and ends the calling script with exit code 1 when plan.md has no such section.
function Get-PlanCycle([string[]] $PlanLines, [int] $Cycle) {
  $headings = for ($i = 0; $i -lt $PlanLines.Count; $i++) {
    $match = [regex]::Match($PlanLines[$i], '^### Cycle (\d+)\s*$')
    if ($match.Success) { [pscustomobject]@{ Number = [int]$match.Groups[1].Value; Index = $i } }
  }
  if (-not $headings) { Write-Error 'plan.md has no "### Cycle N" section.' -ErrorAction Continue; exit 1 }
  if ($Cycle -eq 0) { $Cycle = ($headings | Measure-Object Number -Maximum).Maximum }
  $heading = $headings | Where-Object Number -eq $Cycle | Select-Object -First 1
  if (-not $heading) { Write-Error "plan.md has no section for cycle $Cycle." -ErrorAction Continue; exit 1 }

  $end = $PlanLines.Count
  for ($i = $heading.Index + 1; $i -lt $PlanLines.Count; $i++) {
    if ($PlanLines[$i] -match '^#{1,3} ') { $end = $i; break }
  }
  $section = if ($end -gt $heading.Index + 1) { @($PlanLines[($heading.Index + 1)..($end - 1)]) } else { @() }
  [pscustomobject]@{ Number = $Cycle; Section = $section }
}

# The step blocks of a cycle section. A block starts at "- [<mark>] Step <n>" and ends at the next step or at a
# "####" heading (e.g., the Deviations of the cycle), so a line under "#### Deviations" never counts for a step.
# Wrap the call in @(): one step comes back as a single object.
function Get-CycleSteps([string[]] $Section) {
  $steps = [Collections.Generic.List[object]]::new()
  $isInSteps = $true
  foreach ($line in $Section) {
    if ($line -match '^#### ') { $isInSteps = $false; continue }
    $match = [regex]::Match($line, '^- \[(.)\] Step (\d+)')
    if ($match.Success) {
      $isInSteps = $true
      $steps.Add([pscustomobject]@{
          Mark = $match.Groups[1].Value; Number = $match.Groups[2].Value; Lines = [Collections.Generic.List[string]]::new()
        })
    }
    if ($isInSteps -and $steps.Count -gt 0) { $steps[$steps.Count - 1].Lines.Add($line) }
  }
  $steps
}

# The non-empty lines under "#### Deviations" of a cycle section. Wrap the call in @().
function Get-CycleDeviations([string[]] $Section) {
  $lines = [Collections.Generic.List[string]]::new()
  $isInDeviations = $false
  foreach ($line in $Section) {
    if ($line -match '^#### ') { $isInDeviations = $line -match '^#### Deviations\s*$'; continue }
    if ($isInDeviations -and $line -match '\S') { $lines.Add($line) }
  }
  $lines
}
