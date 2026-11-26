#Requires -Version 7
# Stop and SubagentStop hook: formats the backend changed files, builds the solution, and reports build
# warnings and errors in those files back to the agent that stops. The diagnostics come from the SARIF files of the
# build (Get-SarifDiagnostics), so a changed file of a project that MSBuild skipped as up to date still reports its
# warnings. Build text warnings (NU and MSB) are left out; restore and MSBuild errors count. A line over
# max_line_length is the build warning S103 (backend/SonarLint.xml). It also reports a named index argument
# (NAMED_INDEX, Get-NamedIndexArguments). Exit code 2 blocks the stop and sends stderr to
# that agent, so a subagent fixes its own diagnostics before it reports to the main session.
# The hook uses git only. It verifies the checkout (the main one or a git worktree) that holds this script file:
# "git -C <script folder> rev-parse --show-toplevel". Claude Code runs the script of the project root in which the
# session started, and that root never changes during a session. The hook never reads the hook input cwd (the
# session can move to a folder outside the checkout or into another checkout), CLAUDE_PROJECT_DIR, or a transcript.
# So a session verifies its own checkout only: start each feature session in its feature worktree
# (start-feature.ps1).
# State: each step (backend, frontend, keycloak-theme) keeps the tree ID of its last clean run in the git directory of
# the checkout (claude-verify-<step>-tree.txt, Get-TreeId of checkout-helpers.ps1). The changed files of a step are the
# paths of "git diff --name-only <that tree> <current tree>" that match its patterns, so uncommitted changes that an
# earlier stop verified do not trigger a new run. Without a state tree (or when git gc pruned it), they are the files
# changed since HEAD plus the untracked files. A failed step keeps its old tree, so the next stop checks every file that
# changed since its last clean run again. A passing run of run-area.ps1 or run-tests.ps1 writes the state of its area too
# (Write-HookState), so the hook skips what the run verified. Limit: sessions that share one checkout share this state and these files.
# Give each session its own worktree to separate them.
# One verification or cleanup per checkout runs at a time (named mutex), and the cleanup never overwrites a file
# that another writer changed or deleted during the run (cleanup-changed.ps1).
# The hook does nothing (no build, no cleanup, no report) when the tree ID of the working tree equals the one that
# record-turn-start.ps1 stored at the start of the turn (UserPromptSubmit, by session_id), or the tree ID of the last
# clean verification of the checkout (claude-verify-tree.txt).
# The hook blocks once per stop attempt. When stop_hook_active is set, it reports and lets the turn end,
# so a warning Claude cannot fix does not loop.
# The reviewer never edits the repo, so the hook does nothing at the SubagentStop of the reviewer subagent (agent_type
# "reviewer", .claude/agents/reviewer.md) and in a session started by hand with HEALTHYFAC_SESSION_ROLE=reviewer.
# The Stop of the main session does nothing while an executor subagent is in flight (an entry of background_tasks with
# type "subagent" and agent_type "executor"): the executor verifies its own files at its SubagentStop, and a build of
# its half-done edits would report them to the spec writer, which must not fix them.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'checkout-helpers.ps1')
. (Join-Path $PSScriptRoot 'command-line-batches.ps1')

$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
if ($env:HEALTHYFAC_SESSION_ROLE -eq 'reviewer') { exit 0 }
if ($hookInput.hook_event_name -eq 'SubagentStop' -and $hookInput.agent_type -eq 'reviewer') { exit 0 }
$isExecutorInFlight = $hookInput.hook_event_name -eq 'Stop' -and
  @($hookInput.background_tasks | Where-Object { $_.type -eq 'subagent' -and $_.agent_type -eq 'executor' }).Count -gt 0
if ($isExecutorInFlight) { exit 0 }
$solutionPath = 'backend/HealthyFac.slnx'
$BackendPatterns = $script:BackendPatterns
$WebPatterns = $script:WebPatterns

# The checkout (main or worktree) that holds the directory, in the backslash form that the build diagnostics use,
# or nothing when the directory is not inside a checkout.
function Get-WorktreeRoot([string] $directory) {
  if (-not $directory -or -not (Test-Path -LiteralPath $directory -PathType Container)) { return }
  $root = git -C $directory rev-parse --show-toplevel 2>$null
  if ($LASTEXITCODE -ne 0 -or -not $root) { return }
  [IO.Path]::GetFullPath($root).TrimEnd([char]92, [char]47)
}

function Get-ExistingFiles([string] $repo, [string[]] $paths) {
  @($paths | Where-Object { Test-Path -LiteralPath (Join-Path $repo $_) -PathType Leaf })
}

# Formats the changed files of a web project (frontend or keycloak-theme) with Prettier, then lints the project. A lint
# failure lists the ESLint output lines. The hook runs no build or test.
function Invoke-FrontendVerification([string] $repo, [string] $currentTree, [string] $project) {
  $result = [pscustomobject]@{ Repo = $repo; Step = $project; Problems = @(); Notice = $null; IsSkipped = $false }
  $frontend = Join-Path $repo $project
  if (-not (Test-Path (Join-Path $frontend 'node_modules'))) { $result.IsSkipped = $true; return $result }

  $changed = @(Get-StepChanges $repo $project $currentTree $WebPatterns)
  if ($changed.Count -eq 0) { return $result }
  $touched = @(Get-ExistingFiles $repo $changed)

  Push-Location $frontend
  try {
    $relative = @($touched | ForEach-Object { $_.Substring("$project/".Length) })
    foreach ($batch in Split-ArgumentBatches $relative (Get-ArgumentBudget 'npx.cmd')) {
      & npx prettier --write --ignore-unknown @batch 2>&1 | Out-Null
    }
    $output = & npm run lint 2>&1 | ForEach-Object { "$_" }
    $lintFailed = $LASTEXITCODE -ne 0
  }
  finally { Pop-Location }

  if ($lintFailed) {
    $result.Problems = @("Lint failed (npm run lint in $project/):") +
      @($output | Where-Object { $_ -match '\S' -and $_ -notmatch '^> ' } | Select-Object -First 40)
  }
  return $result
}

# Formats and builds one checkout. Returns its diagnostics (empty when clean) and a notice for the transcript.
function Invoke-Verification([string] $repo, [string] $currentTree) {
  $result = [pscustomobject]@{ Repo = $repo; Step = 'backend'; Problems = @(); Notice = $null; IsSkipped = $false }
  $solution = Join-Path $repo $solutionPath

  # Parallel subagents can stop at the same time. A named mutex per checkout runs one verification at a time, so
  # two builds do not lock each other's output files and two runs do not overwrite the state. The wait stays
  # below the hook timeout. A run that the hook timeout kills leaves the mutex abandoned, and the next run takes it.
  $lock = New-CheckoutMutex $repo
  $lockWait = [TimeSpan]::FromSeconds(480)
  $hasLock = try { $lock.WaitOne($lockWait) } catch [Threading.AbandonedMutexException] { $true }
  if (-not $hasLock) {
    $lock.Dispose()
    $result.Notice = "Verification of $repo skipped: another run held the lock for " +
      "$($lockWait.TotalSeconds) seconds. The next stop checks the files again."
    $result.IsSkipped = $true
    return $result
  }

  Push-Location $repo
  try {
    $changed = @(Get-StepChanges $repo 'backend' $currentTree $BackendPatterns)
    if ($changed.Count -eq 0) { return $result }
    $touched = @(Get-ExistingFiles $repo $changed)

    $touchedCode = @($touched | Where-Object { $_ -like '*.cs' })
    if ($touchedCode.Count -gt 0) {
      $cleanupNotes = @(& (Join-Path $PSScriptRoot 'cleanup-changed.ps1') -Files $touchedCode -Repo $repo |
          Where-Object { $_ -like 'Cleanup *' })
      if ($cleanupNotes.Count -gt 0) { $result.Notice = $cleanupNotes -join [Environment]::NewLine }
    }

    # The hook builds into its own artifacts folder (gitignored, outside the backend Docker context). A test run in
    # another session locks its DLLs in bin/, and a build into the same folder then fails with MSB3027 and blocks
    # the stop for a reason the agent cannot fix. Each worktree has its own folder, so worktrees never share files.
    $artifacts = Join-Path $repo 'artifacts/claude-verify'
    $buildStartedAt = [DateTime]::UtcNow
    $output = & dotnet build $solution -nologo -clp:NoSummary --artifacts-path $artifacts 2>&1 |
      ForEach-Object { "$_" }
    $buildFailed = $LASTEXITCODE -ne 0

    # After a failed build, a project that MSBuild skipped keeps its old SARIF file, so only the files of this build
    # count. A passing build keeps every file: a project that MSBuild skipped as up to date still reports the
    # warnings of its changed file.
    $sinceArguments = if ($buildFailed) { @{ Since = $buildStartedAt } } else { @{} }
    $sarifRoot = Join-Path $artifacts 'obj'
    $diagnostics = @(Get-SarifDiagnostics -Repo $repo -Root $sarifRoot -BuildOutput $output -BuildOutputLevel error `
        -Files $touched @sinceArguments)
    if ($buildFailed) {
      # A failed build reports the errors of every file (a changed signature breaks a caller in an unchanged file) and
      # the warnings of the changed files.
      $diagnostics = @($diagnostics + @(Get-SarifDiagnostics -Repo $repo -Root $sarifRoot @sinceArguments |
          Where-Object { $_ -match '\): error ' }) | Sort-Object -Unique)
    }

    if ($buildFailed -and $diagnostics.Count -eq 0) { $diagnostics = @($output | Select-Object -Last 20) }
    $namedIndexes = @(Get-NamedIndexArguments -Repo $repo -Files $touchedCode)
    $result.Problems = @($diagnostics + $namedIndexes)
    return $result
  }
  finally {
    Pop-Location
    $lock.ReleaseMutex()
    $lock.Dispose()
  }
}

$worktree = Get-WorktreeRoot $PSScriptRoot
if (-not $worktree -or -not (Test-Path (Join-Path $worktree $solutionPath))) { exit 0 }

# The skip: no file changed since the start of the turn, or since the last clean verification.
$currentTree = try { Get-TreeId -Repo $worktree } catch { $null }
if (-not $currentTree) {
  Write-Output "Verification of $worktree skipped: git could not read the tree of the checkout."
  exit 0
}
$turnPath = if ($hookInput.session_id) { Get-TurnTreePath $worktree $hookInput.session_id }
$verifiedPath = Get-GitStatePath $worktree 'claude-verify-tree.txt'
$isUnchangedInTurn = $turnPath -and (Test-Path -LiteralPath $turnPath) -and
  ((Get-Content -LiteralPath $turnPath -Raw).Trim() -eq $currentTree)
$isUnchangedSinceVerified = (Test-Path -LiteralPath $verifiedPath) -and
  ((Get-Content -LiteralPath $verifiedPath -Raw).Trim() -eq $currentTree)
if ($isUnchangedInTurn -or $isUnchangedSinceVerified) { exit 0 }

# One lock around the whole run: the frontend step formats live files, and the state is saved from the tree after
# every step, so both belong to the same critical section as the backend step. The mutex is re-entrant, so
# Invoke-Verification and the cleanup take it again in the same thread.
$outerLock = New-CheckoutMutex $worktree
$hasOuterLock = try { $outerLock.WaitOne([TimeSpan]::FromSeconds(480)) } catch [Threading.AbandonedMutexException] { $true }
if (-not $hasOuterLock) {
  $outerLock.Dispose()
  Write-Output "Verification of $worktree skipped: another run held the lock for 480 seconds. The next stop checks the files again."
  exit 0
}
try {
  $results = @(
    Invoke-Verification $worktree $currentTree
    Invoke-FrontendVerification $worktree $currentTree 'frontend'
    Invoke-FrontendVerification $worktree $currentTree 'keycloak-theme')
  $failed = @($results | Where-Object { $_.Problems.Count -gt 0 })
  $finalTree = try { Get-TreeId -Repo $worktree } catch { $null }
  if ($finalTree) {
    foreach ($result in @($results | Where-Object { -not $_.IsSkipped -and $_.Problems.Count -eq 0 })) {
      Save-StepState $worktree $result.Step $finalTree
    }
    if ($failed.Count -eq 0) {
      New-Item -ItemType Directory -Force (Split-Path $verifiedPath) | Out-Null
      Set-Content -LiteralPath $verifiedPath -Value $finalTree -Encoding utf8NoBOM
    }
  }
}
finally {
  $outerLock.ReleaseMutex()
  $outerLock.Dispose()
}
$results | Where-Object Notice | ForEach-Object { Write-Output $_.Notice }
if ($failed.Count -eq 0) { exit 0 }

$report = @(
  'The build, the named index check, or the frontend lint reports problems in the changed files of the checkout. ' +
    'Fix every item before you finish.'
  foreach ($result in $failed) {
    "Changed files in $($result.Repo):"
    $result.Problems
  }
) -join [Environment]::NewLine

if ($hookInput.stop_hook_active) {
  Write-Output "Verification still fails after one fix attempt:$([Environment]::NewLine)$report"
  exit 0
}

[Console]::Error.WriteLine($report)
exit 2
