# plan-sections.ps1 - dot-sourced by check-plan.ps1, check-handoff.ps1, and commit-cycle.ps1. It reads one cycle of a
# plan.md, so the three scripts agree on what a cycle, a step block, the Deviations of a cycle, and a browser check
# mark are.

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

# The mark of a browser check step block (its first line says "browser check"): 'waiting' when it is "[!]" with
# "WAITING:", 'blocked' when it is "[!]" with "BLOCKED:", else $null (no browser check step, or not "[!]").
function Get-BrowserCheckMark([object] $Step) {
  $first = $Step.Lines[0]
  if ($Step.Mark -ne '!' -or $first -notmatch '(?i)\bbrowser check\b') { return $null }
  if ($first -match '\bWAITING:') { return 'waiting' }
  if ($first -match '\bBLOCKED:') { return 'blocked' }
  return $null
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

# True when the cycle has at least one fixup entry (a Fixup step or an entry of -InlineFixups) and each step that is not
# cancelled ("~") and changes files (its "Files:" line names a path) has a "Fixup:" line. A step without files, such as
# a browser check, does not count. A cycle like that needs no Commit line (.claude/rules/git-commits.md, History).
function Test-FixupOnlyCycle([object[]] $Steps, [object[]] $InlineFixups = @()) {
  $active = @($Steps | Where-Object { $_.Mark -ne '~' })
  $fixupCount = @(Get-StepFixups $active).Count + @($InlineFixups).Count
  if ($fixupCount -eq 0) { return $false }
  $unfixed = @($active | Where-Object { @(Get-StepFiles $_).Count -gt 0 -and -not (Get-StepFixups @($_)) })
  return $unfixed.Count -eq 0
}

# One text per problem of the commit lines of a cycle. -AllowNone drops the "no Commit line." problem (a cycle of only
# fixups); every other problem stays. Wrap the call in @().
function Get-CommitProblems([object[]] $Commits, [switch] $AllowNone) {
  $problems = [Collections.Generic.List[string]]::new()
  if (-not $Commits -or $Commits.Count -eq 0) {
    if (-not $AllowNone) { $problems.Add('no Commit line.') }
    return $problems
  }
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

# The paths of a step block: each backticked path of its "Files:" lines, with "(new)" removed. Wrap the call in @().
function Get-StepFiles([object] $Step) {
  $filesText = ($Step.Lines | Where-Object { $_ -match '^\s*(?:[*-]\s+)?Files:' }) -join ' '
  return @([regex]::Matches(($filesText -replace '\(new\)', ''), '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
}

# The fixup entries of the step blocks: a block with a "Fixup: <subject>" line returns Step (number), Subject, and Files
# (Get-StepFiles). Wrap the call in @().
function Get-StepFixups([object[]] $Steps) {
  foreach ($step in $Steps) {
    if ($step.Mark -eq '~') { continue }
    $fixupLine = $step.Lines | Where-Object { $_ -match '^\s*(?:[*-]\s+)?Fixup:\s*(.+?)\s*$' } | Select-Object -First 1
    if (-not $fixupLine) { continue }
    $subject = [regex]::Match($fixupLine, '^\s*(?:[*-]\s+)?Fixup:\s*(.+?)\s*$').Groups[1].Value
    [pscustomobject]@{ Step = "$($step.Number)"; Subject = $subject; Files = @(Get-StepFiles $step) }
  }
}

# The fixup entries of the Deviations lines: an entry is its "*   Inline fix" line plus the following lines up to the
# next "*   " line or heading. An entry that holds "Fixup: <subject>; files: `<path>`, ..." returns Subject and Files.
# Wrap the call in @().
function Get-InlineFixups([string[]] $Deviations) {
  $entries = [Collections.Generic.List[string]]::new()
  foreach ($line in $Deviations) {
    if ($line -match '^\*\s+Inline fix') { $entries.Add($line.TrimEnd()); continue }
    if ($line -match '^(\*\s|#)') { $entries.Add(''); continue }
    if ($entries.Count -gt 0 -and $entries[$entries.Count - 1] -ne '') { $entries[$entries.Count - 1] += ' ' + $line.Trim() }
  }
  foreach ($line in $entries) {
    if ($line -notmatch '^\*\s+Inline fix') { continue }
    $match = [regex]::Match($line, 'Fixup:\s*(.+?);\s*files:\s*(.*)$')
    if (-not $match.Success) { continue }
    $files = @([regex]::Matches($match.Groups[2].Value, '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
    [pscustomobject]@{ Subject = $match.Groups[1].Value; Files = $files }
  }
}

# The SHA of the first "Reviewed by Artur: <40-hex>" line in the first 20 lines of plan.md, else $null.
function Get-ReviewedMark([string[]] $PlanLines) {
  foreach ($line in ($PlanLines | Select-Object -First 20)) {
    $match = [regex]::Match($line, '^Reviewed by Artur:\s*([0-9a-f]{40})\s*$')
    if ($match.Success) { return $match.Groups[1].Value }
  }
  return $null
}

# The SHA of the first "Base: <sha>" line in the first 20 lines of plan.md, else $null.
function Get-PlanBase([string[]] $PlanLines) {
  foreach ($line in ($PlanLines | Select-Object -First 20)) {
    $match = [regex]::Match($line, '^Base:\s*([0-9a-f]{7,40})\s*$')
    if ($match.Success) { return $match.Groups[1].Value }
  }
  return $null
}

# One text per fixup entry (Get-StepFixups or Get-InlineFixups output) whose Subject matches no logged commit subject or
# 2 or more: "[Step N: ]the Fixup subject '<subject>' matches no commit of <Base>..HEAD." (a step entry names its step;
# "matches <n> commits" for 2 or more). Subjects are the commit subjects of Base..HEAD. The caller adds any prefix.
# Wrap the call in @().
function Get-FixupSubjectProblems([object[]] $Entries, [string[]] $Subjects, [string] $Base) {
  foreach ($entry in @($Entries)) {
    $matchCount = @($Subjects | Where-Object { $_ -ceq $entry.Subject }).Count
    if ($matchCount -eq 1) { continue }
    $stepName = if ($entry.PSObject.Properties['Step']) { "Step $($entry.Step): " } else { '' }
    $matchText = if ($matchCount -eq 0) { 'no commit' } else { "$matchCount commits" }
    "${stepName}the Fixup subject '$($entry.Subject)' matches $matchText of ${Base}..HEAD."
  }
}

# One text per path that two fixup entries with different subjects claim ("Cycle N: <path> is in the Fixup entries of
# '<A>' and '<B>'."), and per path of an inline fix entry that a step without "Fixup:" also lists ("Cycle N: <path> is
# in an inline fix with Fixup and in Step T without Fixup."), and per path of a Fixup step that a step without "Fixup:"
# also lists ("Cycle N: Step S: <path> is in a Fixup step and in Step T without Fixup."). The entries are the output of Get-StepFixups and
# Get-InlineFixups. Wrap the call in @().
function Get-FixupPathProblems([int] $Cycle, [object[]] $Steps, [object[]] $InlineFixups) {
  $entries = @(Get-StepFixups $Steps) + @($InlineFixups)
  $subjectsByPath = [ordered]@{}
  foreach ($entry in $entries) {
    foreach ($path in @($entry.Files)) {
      if (-not $subjectsByPath.Contains($path)) { $subjectsByPath[$path] = [Collections.Generic.List[string]]::new() }
      if ($entry.Subject -notin $subjectsByPath[$path]) { $subjectsByPath[$path].Add($entry.Subject) }
    }
  }
  foreach ($path in $subjectsByPath.Keys) {
    $subjects = $subjectsByPath[$path]
    for ($first = 0; $first -lt $subjects.Count; $first++) {
      for ($second = $first + 1; $second -lt $subjects.Count; $second++) {
        "Cycle ${Cycle}: $path is in the Fixup entries of '$($subjects[$first])' and '$($subjects[$second])'."
      }
    }
  }
  $fixupNumbers = @(Get-StepFixups $Steps | ForEach-Object { $_.Step })
  foreach ($fixup in @(Get-StepFixups $Steps)) {
    foreach ($path in @($fixup.Files)) {
      foreach ($step in ($Steps | Where-Object { $_.Number -notin $fixupNumbers -and $_.Mark -ne '~' })) {
        if ($path -in @(Get-StepFiles $step)) {
          "Cycle ${Cycle}: Step $($fixup.Step): $path is in a Fixup step and in Step $($step.Number) without Fixup."
        }
      }
    }
  }
  foreach ($inline in $InlineFixups) {
    foreach ($path in @($inline.Files)) {
      foreach ($step in ($Steps | Where-Object { $_.Number -notin $fixupNumbers -and $_.Mark -ne '~' })) {
        if ($path -in @(Get-StepFiles $step)) {
          "Cycle ${Cycle}: $path is in an inline fix with Fixup and in Step $($step.Number) without Fixup."
        }
      }
    }
  }
}
