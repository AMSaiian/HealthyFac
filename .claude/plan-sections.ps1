# plan-sections.ps1 - dot-sourced by check-plan.ps1 and check-handoff.ps1. It reads one cycle of a plan.md, so both
# checks agree on what a cycle, a step block, and the Deviations of a cycle are.

# The lines of "### Cycle <N>" up to the next heading of level 1 to 3. -Cycle 0 means the open cycle: the number of
# the first "Cycle: <N>" line of plan.md, or the highest cycle when plan.md has no such line.
# It writes an error and ends the calling script with exit code 1 when plan.md has no such section.
function Get-PlanCycle([string[]] $PlanLines, [int] $Cycle) {
  $headings = for ($i = 0; $i -lt $PlanLines.Count; $i++) {
    $match = [regex]::Match($PlanLines[$i], '^### Cycle (\d+)\s*$')
    if ($match.Success) { [pscustomobject]@{ Number = [int]$match.Groups[1].Value; Index = $i } }
  }
  if (-not $headings) { Write-Error 'plan.md has no "### Cycle N" section.' -ErrorAction Continue; exit 1 }
  if ($Cycle -eq 0) {
    $cycleLine = $PlanLines | Where-Object { $_ -match '^Cycle:\s*(\d+)\s*$' } | Select-Object -First 1
    $Cycle = if ($cycleLine) { [int]([regex]::Match($cycleLine, '^Cycle:\s*(\d+)').Groups[1].Value) } else {
      ($headings | Measure-Object Number -Maximum).Maximum
    }
  }
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

# The "Commit <group>: <subject>" lines of a cycle section, read before its first "####" heading. Wrap the call in @().
function Get-CycleCommits([string[]] $Section) {
  foreach ($line in $Section) {
    if ($line -match '^#### ') { break }
    $match = [regex]::Match($line, '^Commit (\S+):\s*(.*)$')
    if ($match.Success) { [pscustomobject]@{ Group = $match.Groups[1].Value; Subject = $match.Groups[2].Value } }
  }
}

# The commit group of a changed path (it uses "/"): tooling, docs, or code.
function Get-CommitGroup([string] $Path) {
  if ($Path -match '^(\.claude/|scripts/|CLAUDE\.md$)') { return 'tooling' }
  if ($Path -match '^(docs/|README\.md$)') { return 'docs' }
  return 'code'
}

# One text per problem of the commit lines of a cycle. Wrap the call in @().
function Get-CommitProblems([object[]] $Commits) {
  $problems = [Collections.Generic.List[string]]::new()
  if (-not $Commits -or $Commits.Count -eq 0) { $problems.Add('no Commit line.'); return $problems }
  foreach ($commit in $Commits) {
    if ($commit.Group -notin @('tooling', 'docs', 'code')) { $problems.Add("unknown Commit group '$($commit.Group)'.") }
  }
  foreach ($group in ($Commits | Group-Object Group | Where-Object Count -ge 2)) {
    $problems.Add("$($group.Count) Commit lines for the group '$($group.Name)'.")
  }
  foreach ($commit in $Commits) {
    $match = [regex]::Match($commit.Subject, '^(feature|bug)\(([a-z0-9-]+)\): [^A-Z\s][^A-Z]*$')
    if (-not $match.Success) {
      $problems.Add("the Commit subject '$($commit.Subject)' is not '<feature|bug>(<category>): <message in lower case>'.")
      continue
    }
    $category = $match.Groups[2].Value
    if ($commit.Group -eq 'tooling' -and $category -ne 'setup') { $problems.Add('the Commit tooling subject needs the category setup.') }
    if ($commit.Group -eq 'docs' -and $category -ne 'docs') { $problems.Add('the Commit docs subject needs the category docs.') }
  }
  return $problems
}
