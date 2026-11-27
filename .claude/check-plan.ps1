#Requires -Version 7
# check-plan.ps1 - checks the one thing about a cycle that a model cannot check reliably: that the sessions can find
# every part of the cycle, and that each step holds its own facts (.claude/commands/spec.md, plan.md).
# Usage: pwsh .claude/check-plan.ps1 <feature folder> [-Cycle <N>]
#        (default: the "Cycle:" line of plan.md; the highest cycle when plan.md has none)
#        [-Repo <path>] (default: the checkout that holds this script file; it serves the git calls)
#        [-MainBranch <name>] (default: develop; the base without a "Base:" line is the merge base with origin/<name>)
# Output: one line for each problem. Exit code 1 when there is one.
# Problems:
# - the cycle has no "Spec:" line;
# - a section name of the Spec line (in **bold**) is no heading of spec.md;
# - an AC of the Spec line has no "*   **AC<n>:**" line in spec.md, or no row in "## Acceptance Criteria Map";
# - a step names "spec.md" or "the spec" outside the Spec line (the step is not self-contained); the repo path
#   ".claude/commands/spec.md" is exempt, and so are the "Red:" and "Proof:" lines (they quote test and assertion text);
# - a step whose Files: line names a test file, without "TDD", "Proof required", or "No proof: <reason>" in its block;
# - a step whose Verify: bullet asks for a full run ("full", at most 2 words, then run, suite, area, or commands): the
#   last cycle holds the full run (run-area.ps1) and each cycle before it ends with the affected run (run-tests.ps1);
# - the cycle has no "#### Deviations" heading (the cycle keeps its own Deviations, so one range read holds them);
# - an AC number with more than one "*   **AC<n>:**" line in spec.md (a number is never reused);
# - the Commit lines of the cycle ("Commit <tooling|docs|code>: <subject>"): none, an unknown group, 2 lines for one
#   group, a subject that is not "<feature|bug>(<category>): <message in lower case>", a tooling subject without the
#   category setup, or a docs subject without the category docs; a cycle needs no Commit line when it has 1 or more fixup
#   entries (Fixup steps or inline fix entries) and each step (not cancelled) whose Files: line names a path has a
#   "Fixup:" line; a step without files (Files: none, such as a browser check) does not count (Test-FixupOnlyCycle);
# - a step whose Files: line names a template or a style file (.html, .css, "template"), without a "Look:" line in its
#   block (.claude/browser-checks.md, Looking);
# - a step with a "Fixup: <subject>" line, or an "Inline fix" entry with one, whose subject matches no commit, or 2 or more commits, of <base>..HEAD (the
#   base is the "Base:" SHA of plan.md, else the merge base with origin/<-MainBranch>, or with the local branch when
#   origin has none: Get-FeatureBase);
# - a path of a "Fixup:" step that is also in the Files: line of a step of the same cycle without "Fixup:";
# - a path in two fixup entries (steps or inline fix entries) with different subjects, or in an inline fix with Fixup and
#   in a step without "Fixup:" (Get-FixupPathProblems);
# - a "Reviewed by Artur:" SHA in the plan.md header that is no ancestor of HEAD;
# - a "Browser check: none" line of the cycle that quotes the same reason ("...") as such a line of another cycle: a
#   "no UI check" note holds for one cycle (.claude/commands/spec.md, plan.md).
param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [int]$Cycle = 0,
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [string]$MainBranch = 'develop'
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
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
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
$FullRunPattern = '(?i)\bfull(?:\W+\w+){0,2}\W+(?:runs?|suites?|areas?|commands)\b'
$ProofLinePattern = Get-RedProofLinePattern
$VerifyLinePattern = '^\s*(?:[*-]\s+)?\**Verify\**:'
$LookFilePattern = '(?i)\.(html|css)\b|\btemplates?\b'
$LookLinePattern = '^\s*(?:[*-]\s+)?\**Look\**:\s*\S'
$NoBrowserCheckPattern = '(?i)\bBrowser check:\s*none\b'
foreach ($step in $steps) {
  $text = $step.Lines -join "`n"
  $textWithoutProofLines = ($step.Lines | Where-Object { $_ -cnotmatch $ProofLinePattern }) -join "`n"
  $textWithoutCommand = $textWithoutProofLines -replace '(?i)\.claude/commands/spec\.md', ''
  if ($textWithoutCommand -match '(?i)spec\.md|\bthe spec\b') {
    $problems.Add("Cycle ${Cycle}: Step $($step.Number) names the spec. Copy the facts into the step.")
  }
  $filesText = ($step.Lines | Where-Object { $_ -match 'Files:' }) -join "`n"
  if ($filesText -match $TestFilePattern -and $text -cnotmatch '\bTDD\b|\bProof required\b|\bNo proof:') {
    $problems.Add("Cycle ${Cycle}: Step $($step.Number) changes a test file, but says neither TDD, Proof required, nor No proof: <reason>.")
  }
  $verifyText = ($step.Lines | Where-Object { $_ -match $VerifyLinePattern }) -join "`n"
  if ($verifyText -match $FullRunPattern) {
    $problems.Add("Cycle ${Cycle}: Step $($step.Number) asks for a full run in its Verify line. Name test filters; the executor runs the full run once.")
  }
  if ($filesText -match $LookFilePattern -and -not ($step.Lines | Where-Object { $_ -match $LookLinePattern })) {
    $problems.Add("Cycle ${Cycle}: Step $($step.Number) changes a template or a style file, but has no Look: line (as develop <file>, as <page>, or new, Artur: `"<words>`").")
  }
}

$stepFixups = @(Get-StepFixups $steps)

$inlineFixups = @(Get-InlineFixups @(Get-CycleDeviations $section))
if ($stepFixups.Count -gt 0 -or $inlineFixups.Count -gt 0) {
  $base = Get-FeatureBase -Repo $Repo -PlanLines $planLines -MainBranch $MainBranch
  $loggedSubjects = if ($base) { @(git -C $Repo log --format=%s "$($base.Trim())..HEAD" 2>$null) } else { @() }
  foreach ($text in @(Get-FixupSubjectProblems -Entries ($stepFixups + $inlineFixups) -Subjects $loggedSubjects -Base $base)) {
    $problems.Add("Cycle ${Cycle}: $text")
  }
}

foreach ($text in @(Get-FixupPathProblems $Cycle $steps $inlineFixups)) { $problems.Add($text) }

$reviewedMark = Get-ReviewedMark $planLines
if ($reviewedMark) {
  git -C $Repo merge-base --is-ancestor $reviewedMark HEAD 2>$null
  if ($LASTEXITCODE -ne 0) { $problems.Add("plan.md: the Reviewed by Artur SHA $reviewedMark is no ancestor of HEAD.") }
}

$noCheckReasons = [Collections.Generic.List[object]]::new()
$lineCycle = 0
foreach ($line in $planLines) {
  $heading = [regex]::Match($line, '^### Cycle (\d+)\s*$')
  if ($heading.Success) { $lineCycle = [int]$heading.Groups[1].Value; continue }
  if ($line -match '^#{1,3} ') { $lineCycle = 0; continue }
  if ($lineCycle -eq 0 -or $line -notmatch $NoBrowserCheckPattern) { continue }
  foreach ($quote in [regex]::Matches($line, '"([^"]{8,})"')) {
    $noCheckReasons.Add([pscustomobject]@{ Cycle = $lineCycle; Reason = $quote.Groups[1].Value.Trim() })
  }
}
foreach ($own in @($noCheckReasons | Where-Object Cycle -eq $Cycle | Sort-Object Reason -Unique)) {
  $other = $noCheckReasons | Where-Object { $_.Cycle -ne $Cycle -and $_.Reason -eq $own.Reason } | Select-Object -First 1
  if ($other) {
    $problems.Add("Cycle ${Cycle}: 'Browser check: none' repeats the reason of cycle $($other.Cycle) (`"$($own.Reason)`"). A no-UI-check note holds for one cycle; ask Artur again.")
  }
}

if (-not ($section | Where-Object { $_ -match '^#### Deviations\s*$' })) {
  $problems.Add("Cycle ${Cycle}: no '#### Deviations' heading. End the cycle section with it.")
}

$isFixupOnly = Test-FixupOnlyCycle $steps $inlineFixups
foreach ($text in @(Get-CommitProblems @(Get-CycleCommits $section) -AllowNone:$isFixupOnly)) { $problems.Add("Cycle ${Cycle}: $text") }

$acLines = $specLines | ForEach-Object {
  $match = [regex]::Match($_, '^\*\s+\*\*AC(\d+[a-z]*):\*\*')
  if ($match.Success) { $match.Groups[1].Value }
}
foreach ($group in $acLines | Group-Object | Where-Object Count -gt 1) {
  $problems.Add("spec.md has $($group.Count) lines for AC$($group.Name). Never reuse an AC number.")
}

$problems
if ($problems.Count -gt 0) { exit 1 }
