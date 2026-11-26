#Requires -Version 7
# Deterministic check of the Stop hook: the turn skip, the checkout resolution, the lock, and the guard against other
# writers (CLAUDE.md, Verification Hook). It builds throw-away git repositories in a temp folder, replaces the
# JetBrains tool with a fake `jb` that appends "// CLEANED" to every file it is asked to clean, and runs the real hooks.
#
# Cases:
# - Turn skip: a turn that changes no file does nothing; a turn that changes a .cs file runs the cleanup.
# - Reviewer: a session with HEALTHYFAC_SESSION_ROLE=reviewer does nothing; the same Stop without it runs the cleanup.
#   A SubagentStop of the reviewer subagent does nothing; the same SubagentStop of the executor runs the cleanup.
# - Executor in flight: a Stop of the main session with an executor subagent in background_tasks does nothing; the same
#   Stop with only a shell task in flight runs the cleanup.
# - Reviewer write scope: the reviewer PreToolUse hook allows review.md outside the repo and denies a repo file, a
#   review.md inside the repo or one of its worktrees, another file outside the repo, and a call without a file.
# - Handoff write guard: the PreToolUse hook of every session denies each listed shell write form on spec.md, plan.md,
#   and review.md, and allows the reads, a read redirected to another file, .claude/commands/spec.md, and the flow
#   scripts.
# - Step state: after a clean stop, the next stop cleans only the files changed since then; after a lint failure, the
#   next stop formats the same files again; an edit of a file with a non-ASCII name changes the turn tree ID, and the
#   Stop hook cleans that file.
# - Checkout: the hook verifies the checkout that holds its script file. With the session folder (cwd) in checkout 2,
#   outside every checkout, or with CLAUDE_PROJECT_DIR on checkout 2, the hook of checkout 1 cleans checkout 1 only.
#   The hook of a git worktree cleans the worktree and leaves the main checkout unchanged.
# - Lost update: a file that another writer changes during the cleanup keeps that content.
# - Deleted file: a file deleted during the cleanup is not created again.
# - Skipped cleanup: a failed restore of the private copy prints a "Cleanup skipped" line and cleans nothing.
# - Frontend: the step formats changed files, including .webmanifest.
#   A lint line without a capital S reaches the output. A turn tree file older than 1 day goes.
# - Long file lists: with 400 changed files in each area (longer than one command line), the hook passes, and the
#   cleanup and Prettier change every file. The proofs pass the whole list to jb or Prettier on one command line.
# - Lock: two cleanups of one checkout run one after the other; cleanups of two checkouts run at the same time.
#   The cases compare the order of logged events, never durations.
# - Proof: the turn skip case, the checkout case, the two guard cases, and the two step state cases run once more with
#   the skip, the script checkout, the guard, or the state rule switched off in a copy of the hook scripts. The checks
#   must then show the difference, which proves that the switched-off line causes it.
# Run:  pwsh .claude/hooks/tests/hook-scope.tests.ps1
# It exits 1 when a check fails.

$ErrorActionPreference = 'Stop'
$HooksDir = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$work = Join-Path ([IO.Path]::GetTempPath()) "hf-hook-tests-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
New-Item -ItemType Directory -Force $work | Out-Null
$failures = [Collections.Generic.List[string]]::new()
$marker = '// CLEANED'

function Assert-That([bool] $condition, [string] $message) {
  if ($condition) { Write-Host "  ok   $message" } else {
    Write-Host "  FAIL $message" -ForegroundColor Red
    $failures.Add($message)
  }
}

# The fake jb: appends the marker to each --include file under the solution folder. The
# environment steers the cases: FAKE_JB_TOUCH rewrites a live file, FAKE_JB_DELETE deletes one, FAKE_JB_LOG records
# the start and the end of the run (with FAKE_JB_TAG), and FAKE_JB_HOLD names a file that the run waits for.
$fakeBin = Join-Path $work 'bin'
New-Item -ItemType Directory -Force $fakeBin | Out-Null
@'
param()
$include = ($args | Where-Object { $_ -like '--include=*' }) -replace '^--include=', ''
$solution = $args[-1]
$base = Split-Path $solution
if ($env:FAKE_JB_SOLUTION_LOG) { Add-Content $env:FAKE_JB_SOLUTION_LOG $solution }
if ($env:FAKE_JB_LOG) { Add-Content $env:FAKE_JB_LOG "start $([DateTime]::UtcNow.Ticks) $env:FAKE_JB_TAG" }
if ($env:FAKE_JB_HOLD) {
  $deadline = [DateTime]::UtcNow.AddSeconds(180)
  while (-not (Test-Path -LiteralPath $env:FAKE_JB_HOLD) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
}
if ($env:FAKE_JB_TOUCH) { Set-Content -LiteralPath $env:FAKE_JB_TOUCH -Value '// OTHER WRITER' }
if ($env:FAKE_JB_DELETE) { Remove-Item -LiteralPath $env:FAKE_JB_DELETE -Force }
foreach ($file in $include -split ';') {
  $path = Join-Path $base $file
  if (Test-Path -LiteralPath $path) { Add-Content -LiteralPath $path -Value '// CLEANED' }
}
if ($env:FAKE_JB_LOG) { Add-Content $env:FAKE_JB_LOG "end $([DateTime]::UtcNow.Ticks) $env:FAKE_JB_TAG" }
'@ | Set-Content (Join-Path $fakeBin 'fake-jb.ps1')
'@pwsh -NoProfile -File "%~dp0fake-jb.ps1" %*' | Set-Content (Join-Path $fakeBin 'jb.cmd') -Encoding ascii
@'
param()
foreach ($file in $args | Where-Object { $_ -notlike '-*' -and $_ -ne 'prettier' }) {  if (Test-Path -LiteralPath $file) { Add-Content -LiteralPath $file -Value '// PRETTIER' }}
'@ | Set-Content (Join-Path $fakeBin 'fake-npx.ps1')
@'
param()
if ($env:FAKE_LINT_OUTPUT) { Write-Output $env:FAKE_LINT_OUTPUT; exit 1 }
exit 0
'@ | Set-Content (Join-Path $fakeBin 'fake-npm.ps1')
'@pwsh -NoProfile -File "%~dp0fake-npx.ps1" %*' | Set-Content (Join-Path $fakeBin 'npx.cmd') -Encoding ascii
'@pwsh -NoProfile -File "%~dp0fake-npm.ps1" %*' | Set-Content (Join-Path $fakeBin 'npm.cmd') -Encoding ascii
@'
param()
if ($args[0] -eq 'build') {
  if ($env:FAKE_DOTNET_TEXT) { Write-Output ($env:FAKE_DOTNET_TEXT -split '\|') }
  exit ($env:FAKE_DOTNET_BUILD -eq 'fail' ? 1 : 0)
}
& $env:HF_REAL_DOTNET @args
exit $LASTEXITCODE
'@ | Set-Content (Join-Path $fakeBin 'fake-dotnet.ps1')
@'
@if "%FAKE_DOTNET_BUILD%"=="" goto real
@pwsh -NoProfile -File "%~dp0fake-dotnet.ps1" %*
@exit /b %errorlevel%
:real
@"%HF_REAL_DOTNET%" %*
'@ | Set-Content (Join-Path $fakeBin 'dotnet.cmd') -Encoding ascii
$env:HF_REAL_DOTNET = (Get-Command dotnet -CommandType Application | Select-Object -First 1).Source
$env:PATH = "$fakeBin$([IO.Path]::PathSeparator)$env:PATH"

function New-TestRepo([string] $name) {
  $repo = Join-Path $work $name
  $backend = Join-Path $repo 'backend'
  New-Item -ItemType Directory -Force (Join-Path $backend 'App') | Out-Null
  '<Solution><Project Path="App/App.csproj" /></Solution>' | Set-Content (Join-Path $backend 'HealthyFac.slnx')
  '<wpf:ResourceDictionary xmlns:wpf="x" />' | Set-Content (Join-Path $backend 'HealthyFac.sln.DotSettings')
  '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>' |
    Set-Content (Join-Path $backend 'App/App.csproj')
  foreach ($file in 'A', 'B') { "namespace Test; public class $file { }" | Set-Content (Join-Path $backend "App/$file.cs") }
  "artifacts/`nbin/`nobj/`n.claude/" | Set-Content (Join-Path $repo '.gitignore')
  git -C $repo init -q 2>&1 | Out-Null
  git -C $repo config user.email t@t 2>&1 | Out-Null
  git -C $repo config user.name t 2>&1 | Out-Null
  git -C $repo config core.autocrlf false 2>&1 | Out-Null
  git -C $repo add -A 2>&1 | Out-Null
  git -C $repo commit -q -m base 2>&1 | Out-Null
  foreach ($file in 'A', 'B') { Add-Content (Join-Path $backend "App/$file.cs") '// edited' }
  $repo
}

# Runs a hook script as Claude Code does: from the .claude/hooks folder of the checkout. It copies the scripts of
# $hooks (the real ones, or a modified copy) into the checkout first; the test repositories ignore .claude/, so the
# copy changes no git status. $cwd is the session folder of the hook input (default: the checkout).
function Invoke-Hook(
  [string] $repo, [string] $session = 'test', [string] $hooks = $HooksDir, [string] $script = 'verify-changed.ps1',
  [string] $cwd = $repo, [hashtable] $fields = @{}) {
  $installed = Join-Path $repo '.claude/hooks'
  New-Item -ItemType Directory -Force $installed | Out-Null
  Copy-Item (Join-Path $hooks '*.ps1') $installed -Force
  $hookFields = @{ session_id = $session; cwd = $cwd }
  foreach ($key in $fields.Keys) { $hookFields[$key] = $fields[$key] }
  $inputJson = $hookFields | ConvertTo-Json -Compress -Depth 5
  $output = $inputJson | & pwsh -NoProfile -File (Join-Path $installed $script) 2>&1
  [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
}

function Invoke-Cleanup([string] $script, [string] $repo, [string[]] $files) {
  $list = ($files | ForEach-Object { "'$_'" }) -join ','
  & pwsh -NoProfile -Command "& '$script' -Files $list -Repo '$repo'" 2>&1 | Out-Null
}

function Test-Cleaned([string] $repo, [string] $file) {
  (Get-Content (Join-Path $repo "backend/App/$file.cs") -Raw) -match [regex]::Escape($marker)
}

# A copy of the hook scripts with one line replaced, so a check can show what the line prevents.
function New-ModifiedHooks([string] $name, [string] $script, [string] $line, [string] $replacement) {
  $dir = Join-Path $work $name
  New-Item -ItemType Directory -Force $dir | Out-Null
  Copy-Item (Join-Path $HooksDir '*.ps1') $dir
  $path = Join-Path $dir $script
  $text = Get-Content $path -Raw
  if (-not $text.Contains($line)) { throw "The line to switch off is missing in ${script}: update the proof in this test." }
  $text.Replace($line, $replacement) | Set-Content $path
  $dir
}

try {
  Write-Host 'Turn skip'
  $repo = New-TestRepo 'turn-none'
  [void](Invoke-Hook $repo 's1' $HooksDir 'record-turn-start.ps1')
  $result = Invoke-Hook $repo 's1'
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output) 'A turn that changes no file exits 0 and prints nothing'
  Assert-That (-not (Test-Cleaned $repo 'A') -and -not (Test-Cleaned $repo 'B')) 'A turn that changes no file runs no cleanup'

  $repo = New-TestRepo 'turn-change'
  [void](Invoke-Hook $repo 's1' $HooksDir 'record-turn-start.ps1')
  Add-Content (Join-Path $repo 'backend/App/A.cs') '// changed in the turn'
  [void](Invoke-Hook $repo 's1')
  Assert-That (Test-Cleaned $repo 'A') 'A turn that changes a .cs file runs the cleanup'

  $repo = New-TestRepo 'turn-verified'
  [void](Invoke-Hook $repo 's1')
  Assert-That (Test-Cleaned $repo 'A') 'A first Stop without a stored turn tree runs the cleanup'
  $result = Invoke-Hook $repo 's2'
  $count = ([regex]::Matches((Get-Content (Join-Path $repo 'backend/App/A.cs') -Raw), [regex]::Escape($marker))).Count
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output -and $count -eq 1) 'A Stop on a working tree that was verified already does nothing'

  $noSkip = New-ModifiedHooks 'noskip' 'verify-changed.ps1' `
    'if ($isUnchangedInTurn -or $isUnchangedSinceVerified) { exit 0 }' 'if ($false) { exit 0 }'
  $repo = New-TestRepo 'proof-turn'
  [void](Invoke-Hook $repo 's1' $HooksDir 'record-turn-start.ps1')
  [void](Invoke-Hook $repo 's1' $noSkip)
  Assert-That (Test-Cleaned $repo 'A') 'Proof: with the skip switched off, a turn without changes runs the cleanup'

  Write-Host 'Reviewer session'
  $repo = New-TestRepo 'reviewer'
  $env:HEALTHYFAC_SESSION_ROLE = 'reviewer'
  $result = Invoke-Hook $repo 's1'
  Remove-Item Env:HEALTHYFAC_SESSION_ROLE
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output -and -not (Test-Cleaned $repo 'A')) 'A Stop of a reviewer session does nothing'
  [void](Invoke-Hook $repo 's1')
  Assert-That (Test-Cleaned $repo 'A') 'Proof: the same Stop without the reviewer role runs the cleanup'

  Write-Host 'Reviewer subagent'
  $repo = New-TestRepo 'reviewer-subagent'
  $result = Invoke-Hook $repo 's1' -fields @{ hook_event_name = 'SubagentStop'; agent_type = 'reviewer'; agent_id = 'a1' }
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output -and -not (Test-Cleaned $repo 'A')) 'A SubagentStop of the reviewer does nothing'
  [void](Invoke-Hook $repo 's1' -fields @{ hook_event_name = 'SubagentStop'; agent_type = 'executor'; agent_id = 'a2' })
  Assert-That (Test-Cleaned $repo 'A') 'Proof: the same SubagentStop of the executor runs the cleanup'

  Write-Host 'Executor in flight'
  $repo = New-TestRepo 'executor-in-flight'
  $executorTask = @{ id = 't1'; type = 'subagent'; status = 'running'; description = 'run'; agent_type = 'executor' }
  $result = Invoke-Hook $repo 's1' -fields @{ hook_event_name = 'Stop'; background_tasks = @($executorTask) }
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output -and -not (Test-Cleaned $repo 'A')) 'A Stop of the main session with the executor in flight does nothing'
  $shellTask = @{ id = 't2'; type = 'shell'; status = 'running'; description = 'tail'; command = 'tail -f log' }
  [void](Invoke-Hook $repo 's1' -fields @{ hook_event_name = 'Stop'; background_tasks = @($shellTask) })
  Assert-That (Test-Cleaned $repo 'A') 'Proof: the same Stop with only a shell task in flight runs the cleanup'

  Write-Host 'Reviewer write scope'
  $repo = New-TestRepo 'write-scope'
  $scopeTree = Join-Path $work 'write-scope-feature'
  git -C $repo worktree add -q $scopeTree -b feature/scope 2>&1 | Out-Null
  $featureFolder = Join-Path $work 'specs/HealthyFac/scope'
  New-Item -ItemType Directory -Force $featureFolder | Out-Null
  $writeCases = @(
    @{ Path = (Join-Path $featureFolder 'review.md'); Exit = 0; Name = 'allows review.md in the feature folder outside the repo' },
    @{ Path = (Join-Path $repo 'backend/App/A.cs'); Exit = 2; Name = 'denies a repo file' },
    @{ Path = (Join-Path $repo 'review.md'); Exit = 2; Name = 'denies a review.md inside the repo' },
    @{ Path = (Join-Path $scopeTree 'docs/review.md'); Exit = 2; Name = 'denies a review.md in a new folder of a worktree of the repo' },
    @{ Path = (Join-Path $featureFolder 'plan.md'); Exit = 2; Name = 'denies plan.md in the feature folder' })
  foreach ($case in $writeCases) {
    $result = Invoke-Hook $repo 's1' $HooksDir 'reviewer-write-scope.ps1' -fields @{
      hook_event_name = 'PreToolUse'; tool_name = 'Write'; tool_input = @{ file_path = $case.Path; content = 'x' } }
    Assert-That ($result.ExitCode -eq $case.Exit) "The reviewer write hook $($case.Name)"
  }
  $result = Invoke-Hook $repo 's1' $HooksDir 'reviewer-write-scope.ps1' -fields @{
    hook_event_name = 'PreToolUse'; tool_name = 'NotebookEdit'; tool_input = @{ notebook_path = (Join-Path $repo 'n.ipynb') } }
  Assert-That ($result.ExitCode -eq 2) 'The reviewer write hook denies a notebook in the repo'
  $result = Invoke-Hook $repo 's1' $HooksDir 'reviewer-write-scope.ps1' -fields @{ hook_event_name = 'PreToolUse'; tool_name = 'Write' }
  Assert-That ($result.ExitCode -eq 2) 'The reviewer write hook denies a call without a file'

  Write-Host 'Handoff write guard'
  $repo = New-TestRepo 'handoff-guard'
  $handoff = 'E:/specs/HealthyFac/feature'
  function Invoke-ShellHook([string] $script, [string] $command, [string] $tool = 'Bash') {
    Invoke-Hook $repo 's1' $HooksDir $script -fields @{
      hook_event_name = 'PreToolUse'; tool_name = $tool; tool_input = @{ command = $command } }
  }
  $denied = @(
    @('a > redirect', "echo x > $handoff/plan.md"),
    @('a >> redirect', "echo x >> `"$handoff/review.md`""),
    @('tee', "echo x | tee -a $handoff/plan.md"),
    @('sed -i', "sed -i 's/a/b/' $handoff/plan.md"),
    @('perl -i', "perl -i -pe 's/a/b/' $handoff/plan.md"),
    @('Set-Content', "Set-Content $handoff/plan.md 'x'"),
    @('Add-Content', "Add-Content $handoff/review.md 'x'"),
    @('Out-File', "'x' | Out-File $handoff/plan.md"),
    @('writeFileSync', "node -e `"require('fs').writeFileSync('$handoff/plan.md', 'x')`""),
    @('appendFileSync', "node -e `"require('fs').appendFileSync('$handoff/plan.md', 'x')`""),
    @('write_text', "python -c `"import pathlib; pathlib.Path('$handoff/spec.md').write_text('x')`""),
    @('.write(', "python - <<'E'`np='$handoff/plan.md'`ns=open(p).read()`nopen(p, 'w').write(s)`nE"),
    @('WriteAllText', "[IO.File]::WriteAllText('$handoff/plan.md', 'x')"),
    @('WriteAllLines', "[IO.File]::WriteAllLines('$handoff/plan.md', @('x'))"),
    @('mv', "mv /tmp/x.md $handoff/plan.md"),
    @('cp', "cp /tmp/x.md $handoff/review.md"),
    @('rm', "rm $handoff/review.md"),
    @('Move-Item', "Move-Item x.md $handoff/plan.md"),
    @('Copy-Item', "Copy-Item x.md $handoff/spec.md"),
    @('Remove-Item', "Remove-Item $handoff/plan.md"))
  foreach ($case in $denied) {
    $result = Invoke-ShellHook 'handoff-write-guard.ps1' $case[1]
    Assert-That ($result.ExitCode -eq 2 -and $result.Output -match 'Edit tool') "The handoff guard denies $($case[0]) on a handoff file"
  }
  $result = Invoke-ShellHook 'handoff-write-guard.ps1' "Set-Content $handoff/plan.md 'x'" 'PowerShell'
  Assert-That ($result.ExitCode -eq 2) 'The handoff guard denies a write in the PowerShell tool'
  $allowed = @(
    @('cat', "cat $handoff/plan.md"),
    @('grep', "grep -n 'Step' $handoff/plan.md"),
    @('head', "head -20 $handoff/plan.md"),
    @('Get-Content', "Get-Content $handoff/plan.md -TotalCount 20"),
    @('Select-String', "Select-String -Path $handoff/plan.md -Pattern 'Step'"),
    @('a read redirected to another file', "grep x $handoff/plan.md > /tmp/out.txt 2>&1"),
    @('sed -i on .claude/commands/spec.md', "sed -i 's/a/b/' .claude/commands/spec.md"),
    @('check-handoff on the folder', "pwsh .claude/check-handoff.ps1 $handoff"),
    @('check-review-routing on review.md', "pwsh .claude/check-review-routing.ps1 $handoff/review.md"))
  foreach ($case in $allowed) {
    $result = Invoke-ShellHook 'handoff-write-guard.ps1' $case[1]
    Assert-That ($result.ExitCode -eq 0) "The handoff guard allows $($case[0])"
  }

  Write-Host 'Checkout resolved from the hook script'
  $first = New-TestRepo 'checkout-one'
  $second = New-TestRepo 'checkout-two'
  [void](Invoke-Hook $first 'test' $HooksDir 'verify-changed.ps1' (Join-Path $second 'backend'))
  Assert-That ((Test-Cleaned $first 'A') -and (Test-Cleaned $first 'B')) 'The hook of checkout 1 cleans checkout 1, with the session folder in checkout 2'
  Assert-That (-not (Test-Cleaned $second 'A') -and -not (Test-Cleaned $second 'B')) 'The hook of checkout 1 leaves checkout 2 unchanged'

  $outside = Join-Path $work 'not-a-checkout'
  New-Item -ItemType Directory -Force $outside | Out-Null
  $third = New-TestRepo 'checkout-outside'
  $env:CLAUDE_PROJECT_DIR = $second
  [void](Invoke-Hook $third 'test' $HooksDir 'verify-changed.ps1' $outside)
  Remove-Item Env:CLAUDE_PROJECT_DIR
  Assert-That (Test-Cleaned $third 'A') 'The hook cleans its checkout, with the session folder outside every checkout'
  Assert-That (-not (Test-Cleaned $second 'A')) 'CLAUDE_PROJECT_DIR on another checkout changes nothing'

  $fromCwd = New-ModifiedHooks 'from-cwd' 'verify-changed.ps1' `
    '$worktree = Get-WorktreeRoot $PSScriptRoot' '$worktree = Get-WorktreeRoot $hookInput.cwd'
  $repo = New-TestRepo 'proof-checkout'
  [void](Invoke-Hook $repo 'test' $fromCwd 'verify-changed.ps1' $outside)
  Assert-That (-not (Test-Cleaned $repo 'A')) 'Proof: with the checkout taken from the session folder, the same Stop cleans nothing'

  Write-Host 'Git worktree'
  $main = New-TestRepo 'worktree-main'
  $feature = Join-Path $work 'worktree-feature'
  git -C $main worktree add -q $feature -b feature/x 2>&1 | Out-Null
  Add-Content (Join-Path $feature 'backend/App/A.cs') '// edited in the worktree'
  [void](Invoke-Hook $feature)
  Assert-That (Test-Cleaned $feature 'A') 'The hook of a worktree cleans the changed file of the worktree'
  Assert-That (-not (Test-Cleaned $main 'A')) 'The hook of a worktree leaves the main checkout unchanged'

  $cleanup = Join-Path $HooksDir 'cleanup-changed.ps1'

  Write-Host 'Short private copy'
  $main = New-TestRepo 'short-main'
  $longName = 'adjust-action-store-to-introduce-correct-return-actions'
  $longFeature = Join-Path $work $longName
  git -C $main worktree add -q $longFeature -b feature/long 2>&1 | Out-Null
  Add-Content (Join-Path $longFeature 'backend/App/A.cs') '// edited in the worktree'
  $commonDir = (git -C $longFeature rev-parse --path-format=absolute --git-common-dir)
  $env:FAKE_JB_SOLUTION_LOG = Join-Path $work 'solution-short.log'
  Invoke-Cleanup $cleanup $longFeature @('backend/App/A.cs')
  Remove-Item Env:FAKE_JB_SOLUTION_LOG
  $copyPath = [IO.Path]::GetFullPath((Get-Content (Join-Path $work 'solution-short.log') | Select-Object -First 1))
  Assert-That ($copyPath.StartsWith([IO.Path]::GetFullPath((Join-Path $commonDir 'cs')), [StringComparison]::OrdinalIgnoreCase)) 'The cleanup copy lives under cs/ of the git common directory'
  Assert-That (-not $copyPath.Contains($longName)) 'The path of the cleanup copy does not hold the worktree name'
  Assert-That (Test-Cleaned $longFeature 'A') 'A file of the long-named worktree is cleaned'

  Write-Host 'Proof: the copy under the old git path holds the worktree name'
  $oldPathDir = New-ModifiedHooks 'old-copy-path' 'cleanup-changed.ps1' `
    '--git-common-dir) ''cs''' '--git-path claude-cleanup-shadow) ''x'''
  $env:FAKE_JB_SOLUTION_LOG = Join-Path $work 'solution-old.log'
  Invoke-Cleanup (Join-Path $oldPathDir 'cleanup-changed.ps1') $longFeature @('backend/App/A.cs')
  Remove-Item Env:FAKE_JB_SOLUTION_LOG
  Assert-That ((Get-Content (Join-Path $work 'solution-old.log') | Select-Object -First 1).Contains($longName)) 'Proof: with the old copy path, the path holds the worktree name'

  Write-Host 'Lost update'
  $repo = New-TestRepo 'lost'
  $live = Join-Path $repo 'backend/App/A.cs'
  $env:FAKE_JB_TOUCH = $live
  Invoke-Cleanup $cleanup $repo @('backend/App/A.cs', 'backend/App/B.cs')
  Remove-Item Env:FAKE_JB_TOUCH
  Assert-That ((Get-Content $live -Raw).Trim() -eq '// OTHER WRITER') 'A file changed during the cleanup keeps the other writer content'
  Assert-That (Test-Cleaned $repo 'B') 'The other file of the same run is still cleaned'

  Write-Host 'Deleted file'
  $repo = New-TestRepo 'deleted'
  $live = Join-Path $repo 'backend/App/A.cs'
  $env:FAKE_JB_DELETE = $live
  Invoke-Cleanup $cleanup $repo @('backend/App/A.cs')
  Remove-Item Env:FAKE_JB_DELETE
  Assert-That (-not (Test-Path $live)) 'A file deleted during the cleanup is not created again'

  Write-Host 'Proof: the same cases with the guard switched off'
  $unguardedDir = New-ModifiedHooks 'unguarded' 'cleanup-changed.ps1' `
    'if ($copied[$target] -ne $before[$target] -or (Get-FileHashText $live) -ne $before[$target]) {' 'if ($false) {'
  $unguarded = Join-Path $unguardedDir 'cleanup-changed.ps1'
  $text = (Get-Content $unguarded -Raw).Replace('if (-not (Test-Path -LiteralPath $live)) {', 'if ($false) {')
  Set-Content $unguarded $text
  $repo = New-TestRepo 'proof-lost'
  $live = Join-Path $repo 'backend/App/A.cs'
  $env:FAKE_JB_TOUCH = $live
  Invoke-Cleanup $unguarded $repo @('backend/App/A.cs')
  Remove-Item Env:FAKE_JB_TOUCH
  Assert-That ((Get-Content $live -Raw).Trim() -ne '// OTHER WRITER') 'Proof: without the guard, the cleanup overwrites the other writer content'
  $repo = New-TestRepo 'proof-deleted'
  $live = Join-Path $repo 'backend/App/A.cs'
  $env:FAKE_JB_DELETE = $live
  Invoke-Cleanup $unguarded $repo @('backend/App/A.cs')
  Remove-Item Env:FAKE_JB_DELETE
  Assert-That (Test-Path $live) 'Proof: without the guard, the cleanup creates the deleted file again'

  Write-Host 'Skipped cleanup is reported'
  $repo = New-TestRepo 'restore-fails'
  Set-Content (Join-Path $repo 'backend/App/App.csproj') '<Project><this is not xml'
  $output = (& pwsh -NoProfile -Command "& '$cleanup' -Files 'backend/App/A.cs' -Repo '$repo'" 2>&1) -join "`n"
  Assert-That ($output -match 'Cleanup skipped: dotnet restore') 'A failed restore of the private copy prints a "Cleanup skipped" line'
  Assert-That (-not (Test-Cleaned $repo 'A')) 'A failed restore of the private copy cleans no file'

  Write-Host 'Lock'
  # The first cleanup holds the lock inside the fake jb until the test writes the release file. The second cleanup
  # starts after the first one logged its start. The test releases the first one only after the second one reported
  # that it waits for the lock (-Verbose), or, with -WaitForSecondStart, after the second one started its own jb.
  # The checks compare the order of the logged events, never durations.
  function Wait-Until([scriptblock] $condition, [string] $what) {
    $deadline = [DateTime]::UtcNow.AddSeconds(180)
    while (-not (& $condition)) {
      if ([DateTime]::UtcNow -gt $deadline) { throw "Timeout while waiting for: $what" }
      Start-Sleep -Milliseconds 100
    }
  }
  function Measure-TwoCleanups(
    [string] $repoOne, [string] $repoTwo, [string] $fileTwo, [string] $script = $cleanup, [switch] $WaitForSecondStart) {
    $id = [Guid]::NewGuid().ToString('N')
    $log = Join-Path $work "lock-$id.log"
    $release = Join-Path $work "lock-$id.release"
    $waiting = Join-Path $work "lock-$id.waiting"
    New-Item -ItemType File $log | Out-Null
    $env:FAKE_JB_LOG = $log
    $env:FAKE_JB_HOLD = $release
    $readLog = { @(Get-Content $log -ErrorAction SilentlyContinue) }
    try {
      $env:FAKE_JB_TAG = 'one'
      $first = Start-Process pwsh -PassThru -WindowStyle Hidden -ArgumentList @(
        '-NoProfile', '-File', $script, '-Files', 'backend/App/A.cs', '-Repo', $repoOne)
      Wait-Until { (& $readLog) -match '^start \d+ one$' } 'the first cleanup holds the lock'

      $env:FAKE_JB_TAG = 'two'
      $second = Start-Process pwsh -PassThru -WindowStyle Hidden -RedirectStandardOutput $waiting -ArgumentList @(
        '-NoProfile', '-File', $script, '-Files', $fileTwo, '-Repo', $repoTwo, '-Verbose')
      Wait-Until { (Get-Content $waiting -ErrorAction SilentlyContinue) -match 'Waiting for the checkout lock' } `
        'the second cleanup reports that it waits'
      if ($WaitForSecondStart) {
        Wait-Until { (& $readLog) -match '^start \d+ two$' } 'the second cleanup starts its jb'
      }
      Set-Content $release 'go'
      $first.WaitForExit()
      $second.WaitForExit()
    }
    finally {
      Remove-Item Env:FAKE_JB_LOG, Env:FAKE_JB_HOLD, Env:FAKE_JB_TAG -ErrorAction SilentlyContinue
    }
    $events = @(& $readLog | ForEach-Object {
        $parts = $_ -split ' '
        [pscustomobject]@{ Kind = $parts[0]; Ticks = [long]$parts[1]; Tag = $parts[2] }
      })
    $secondStart = $events | Where-Object { $_.Kind -eq 'start' -and $_.Tag -eq 'two' }
    $firstEnd = $events | Where-Object { $_.Kind -eq 'end' -and $_.Tag -eq 'one' }
    [pscustomobject]@{
      Runs = @($events | Where-Object Kind -eq 'start').Count
      IsOverlapping = [bool]($secondStart -and $firstEnd -and $secondStart.Ticks -lt $firstEnd.Ticks)
    }
  }
  $same = New-TestRepo 'lock-same'
  $result = Measure-TwoCleanups $same $same 'backend/App/B.cs'
  Assert-That ($result.Runs -eq 2 -and -not $result.IsOverlapping) 'Two cleanups of one checkout run one after the other'
  $one = New-TestRepo 'lock-one'
  $two = New-TestRepo 'lock-two'
  $result = Measure-TwoCleanups $one $two 'backend/App/A.cs' -WaitForSecondStart
  Assert-That ($result.Runs -eq 2 -and $result.IsOverlapping) 'Cleanups of two checkouts do not wait for each other'

  Write-Host 'Turn tree of non-ASCII paths'
  . (Join-Path $HooksDir 'checkout-helpers.ps1')
  $repo = New-TestRepo 'turn-tree-ascii'
  $odd = Join-Path $repo "backend/App/$([char]0x00DC)ber.cs"
  [IO.File]::WriteAllText($odd, 'namespace Test; public class Uber { }')
  $treeBefore = Get-TreeId -Repo $repo
  [IO.File]::WriteAllText($odd, 'namespace Test; public class Uber { public int X; }')
  $treeAfter = Get-TreeId -Repo $repo
  Assert-That ($treeBefore -match '^[0-9a-f]{40}$' -and $treeBefore -ne $treeAfter) 'An edit of an untracked file with a non-ASCII name changes the turn tree ID'
  [void](Invoke-Hook $repo 's1')
  Assert-That (Test-Cleaned $repo "$([char]0x00DC)ber") 'The Stop hook cleans an edited file with a non-ASCII name'

  Write-Host 'Step state as a tree'
  function Get-MarkCount([string] $path, [string] $mark) {
    ([regex]::Matches((Get-Content $path -Raw), [regex]::Escape($mark))).Count
  }
  $repo = New-TestRepo 'state-tree'
  [void](Invoke-Hook $repo 's1')
  Add-Content (Join-Path $repo 'backend/App/B.cs') '// edited after the clean stop'
  [void](Invoke-Hook $repo 's2')
  Assert-That ((Get-MarkCount (Join-Path $repo 'backend/App/B.cs') $marker) -eq 2 -and
    (Get-MarkCount (Join-Path $repo 'backend/App/A.cs') $marker) -eq 1) 'After a clean stop, the next stop cleans only the files changed since then'
  $noState = New-ModifiedHooks 'no-step-state' 'verify-changed.ps1' 'Save-StepState $worktree $result.Step $finalTree' '$null = $result'
  $repo = New-TestRepo 'proof-state-tree'
  [void](Invoke-Hook $repo 's1' $noState)
  Add-Content (Join-Path $repo 'backend/App/B.cs') '// edited after the clean stop'
  [void](Invoke-Hook $repo 's2' $noState)
  Assert-That ((Get-MarkCount (Join-Path $repo 'backend/App/A.cs') $marker) -eq 2) 'Proof: without the saved step tree, the next stop cleans the unchanged file again'

  Write-Host 'Frontend Prettier and lint'
  function New-FrontendRepo([string] $name) {
    $repo = New-TestRepo $name
    Add-Content (Join-Path $repo '.gitignore') 'node_modules/'
    New-Item -ItemType Directory -Force (Join-Path $repo 'frontend/src'), (Join-Path $repo 'frontend/node_modules') | Out-Null
    foreach ($file in 'a', 'b') { "export const $file = 1;" | Set-Content (Join-Path $repo "frontend/src/$file.ts") }
    git -C $repo add -A 2>&1 | Out-Null
    git -C $repo commit -q -m frontend 2>&1 | Out-Null
    foreach ($file in 'a', 'b') { Add-Content (Join-Path $repo "frontend/src/$file.ts") '// edited' }
    $repo
  }
  function Test-Prettiered([string] $repo, [string] $file) {
    (Get-Content (Join-Path $repo "frontend/src/$file.ts") -Raw) -match '// PRETTIER'
  }

  $repo = New-FrontendRepo 'prettier-plain'
  $result = Invoke-Hook $repo
  Assert-That ((Test-Prettiered $repo 'a') -and (Test-Prettiered $repo 'b') -and -not $result.Output) 'The frontend step formats the changed files'

  $repo = New-FrontendRepo 'prettier-manifest'
  New-Item -ItemType Directory -Force (Join-Path $repo 'frontend/public') | Out-Null
  '{ "name": "x" }' | Set-Content (Join-Path $repo 'frontend/public/site.webmanifest')
  [void](Invoke-Hook $repo)
  Assert-That ((Get-Content (Join-Path $repo 'frontend/public/site.webmanifest') -Raw) -match '// PRETTIER') 'The frontend step formats a changed .webmanifest file'

  Write-Host 'Lint output filter'
  $lintLine = '3:7  error  foo  42'
  $repo = New-FrontendRepo 'lint-line'
  $env:FAKE_LINT_OUTPUT = $lintLine
  $result = Invoke-Hook $repo
  Remove-Item Env:FAKE_LINT_OUTPUT
  Assert-That ($result.ExitCode -eq 2 -and $result.Output.Contains($lintLine)) 'A lint line without a capital S reaches the output'
  $oldFilter = New-ModifiedHooks 'old-lint-filter' 'verify-changed.ps1' `
    "`$_ -match '\S' -and" "`$_ -match 'S' -and"
  $repo = New-FrontendRepo 'proof-lint-line'
  $env:FAKE_LINT_OUTPUT = $lintLine
  $result = Invoke-Hook $repo 'test' $oldFilter
  Remove-Item Env:FAKE_LINT_OUTPUT
  Assert-That (-not $result.Output.Contains($lintLine)) 'Proof: with the old pattern S, the same lint line is lost'

  Write-Host 'Failed step keeps its tree'
  $repo = New-FrontendRepo 'lint-keeps-tree'
  $env:FAKE_LINT_OUTPUT = $lintLine
  [void](Invoke-Hook $repo)
  Remove-Item Env:FAKE_LINT_OUTPUT
  [void](Invoke-Hook $repo)
  Assert-That ((Get-MarkCount (Join-Path $repo 'frontend/src/a.ts') '// PRETTIER') -eq 2) 'After a lint failure, the next stop formats the same files again'
  $failedSaves = New-ModifiedHooks 'failed-saves-tree' 'verify-changed.ps1' `
    '-not $_.IsSkipped -and $_.Problems.Count -eq 0 })' '-not $_.IsSkipped })'
  $repo = New-FrontendRepo 'proof-lint-keeps-tree'
  $env:FAKE_LINT_OUTPUT = $lintLine
  [void](Invoke-Hook $repo 'test' $failedSaves)
  Remove-Item Env:FAKE_LINT_OUTPUT
  [void](Invoke-Hook $repo 'test' $failedSaves)
  Assert-That ((Get-MarkCount (Join-Path $repo 'frontend/src/a.ts') '// PRETTIER') -eq 1) 'Proof: when a failed step saves its tree, the next stop skips its files'

  Write-Host 'Long file lists'
  function New-LongListRepo([string] $name) {
    $repo = New-FrontendRepo $name
    $folder = 'generated_with_a_long_folder_name_so_that_the_file_list_passes_the_command_line_limit'
    $backend = New-Item -ItemType Directory -Force (Join-Path $repo "backend/App/$folder")
    $frontend = New-Item -ItemType Directory -Force (Join-Path $repo "frontend/src/$folder")
    foreach ($index in 0..399) {
      $number = $index.ToString('000')
      "namespace Test; public class C$number { }" | Set-Content (Join-Path $backend "Class$number.cs")
      "export const c$number = 1;" | Set-Content (Join-Path $frontend "file-$number.ts")
    }
    $repo
  }
  function Get-UnmarkedCount([string] $repo, [string] $pattern, [string] $mark) {
    @(Get-ChildItem (Join-Path $repo $pattern) | Where-Object { (Get-Content $_.FullName -Raw) -notmatch [regex]::Escape($mark) }).Count
  }

  $repo = New-LongListRepo 'long-lists'
  $result = Invoke-Hook $repo
  Assert-That ($result.ExitCode -eq 0) "The hook passes with 400 changed files in each area (output: $($result.Output))"
  Assert-That ((Get-UnmarkedCount $repo 'backend/App/generated*/*.cs' $marker) -eq 0) 'The cleanup cleans each of 400 changed C# files'
  Assert-That ((Get-UnmarkedCount $repo 'frontend/src/generated*/*.ts' '// PRETTIER') -eq 0) 'Prettier formats each of 400 changed frontend files'

  $oneJbCall = New-ModifiedHooks 'one-jb-call' 'cleanup-changed.ps1' `
    'Split-ArgumentBatches $includes (Get-ArgumentBudget $jb)' ', $includes'
  $repo = New-LongListRepo 'proof-one-jb-call'
  [void](Invoke-Hook $repo 'test' $oneJbCall)
  Assert-That ((Get-UnmarkedCount $repo 'backend/App/generated*/*.cs' $marker) -eq 400) 'Proof: with one jb call for all files, no file is cleaned'

  $onePrettierCall = New-ModifiedHooks 'one-prettier-call' 'verify-changed.ps1' `
    "Split-ArgumentBatches `$relative (Get-ArgumentBudget 'npx.cmd')" ', $relative'
  $repo = New-LongListRepo 'proof-one-prettier-call'
  [void](Invoke-Hook $repo 'test' $onePrettierCall)
  Assert-That ((Get-UnmarkedCount $repo 'frontend/src/generated*/*.ts' '// PRETTIER') -eq 400) 'Proof: with one Prettier call for all files, no file is formatted'

  Write-Host 'SARIF reader'
  function New-SarifRoot([string] $repo, [string] $name) {
    $root = Join-Path $work $name
    New-Item -ItemType Directory -Force $root | Out-Null
    $repoUri = $repo.Replace('\', '/')
    foreach ($sample in Get-ChildItem (Join-Path $PSScriptRoot 'sarif') -Filter '*.sarif') {
      (Get-Content $sample.FullName -Raw).Replace('@REPO@', $repoUri) | Set-Content (Join-Path $root $sample.Name)
    }
    $root
  }
  $sarifRepo = Join-Path $work 'sarif-repo'
  $sarifRoot = New-SarifRoot $sarifRepo 'sarif-root'
  $sarifLines = @(Get-SarifDiagnostics -Repo $sarifRepo -Root $sarifRoot)
  Assert-That ($sarifLines.Count -eq 4) "The reader returns the 4 reported diagnostics of the two sample files ($($sarifLines.Count))"
  Assert-That (-not ($sarifLines -match 'CA1822')) 'The reader skips a suppressed result'
  Assert-That (-not ($sarifLines -match 'CS8618')) 'The reader skips a result under /obj/'
  Assert-That (-not ($sarifLines -match 'IDE0058')) 'The reader skips a result of the level note'
  Assert-That (@($sarifLines -match 'CS0219').Count -eq 1) 'The same result in both files is one line'
  Assert-That ($sarifLines -contains 'backend/Other/B.cs(20,9): warning CS0162: Unreachable code detected') `
    'The path is relative to the repo, and the line has the typed form'
  Assert-That ($sarifLines -contains "backend/App/A.cs(12,4): error CS0168: The variable 'e' is declared but never used") `
    'A message of several lines is one line'
  $buildText = @(
    "$sarifRepo\backend\App\App.csproj : warning NU1903: Package 'X' 1.0 has a known vulnerability. [$sarifRepo\backend\App\App.csproj]",
    'noise: warning CS0000: not an NU or MSB line')
  $withText = @(Get-SarifDiagnostics -Repo $sarifRepo -Root $sarifRoot -BuildOutput $buildText)
  Assert-That ($withText.Count -eq 5 -and (@($withText -match 'NU1903').Count -eq 1)) 'An NU1903 text line is in, without its [project] suffix'
  Assert-That (-not ($withText -match '\[')) 'The text line has no trailing [project]'
  Assert-That ($withText -contains "backend/App/App.csproj : warning NU1903: Package 'X' 1.0 has a known vulnerability.") `
    'The text line of a backslash path loses the backslash repo prefix'
  $slashText = @(Get-SarifDiagnostics -Repo $sarifRepo.Replace('\', '/') -Root $sarifRoot -BuildOutput $buildText)
  Assert-That ($slashText -contains "backend/App/App.csproj : warning NU1903: Package 'X' 1.0 has a known vulnerability.") `
    'The text line of a backslash path loses the repo prefix of a forward-slash -Repo'
  $skipped = 0
  $without = @(Get-SarifDiagnostics -Repo $sarifRepo -Root $sarifRoot -SkipRules 'IDE0007' -SkippedCount ([ref]$skipped))
  Assert-That ($without.Count -eq 3 -and -not ($without -match 'IDE0007') -and $skipped -eq 1) '-SkipRules IDE0007 drops the line and counts 1'
  $onlyA = @(Get-SarifDiagnostics -Repo $sarifRepo -Root $sarifRoot -Files 'backend/App/A.cs')
  Assert-That ($onlyA.Count -eq 2 -and -not ($onlyA -match 'B\.cs|Util\.cs')) '-Files keeps only the results of the named file'
  $sarifRepo2 = New-TestRepo 'cleanup-rules'
  New-Item -ItemType Directory -Force (Join-Path $sarifRepo2 '.claude/hooks') | Out-Null
  "IDE0007`n`nIDE0008`n" | Set-Content (Join-Path $sarifRepo2 '.claude/hooks/cleanup-fixed-rules.txt')
  Assert-That ((@(Get-CleanupFixedRules $sarifRepo2) -join ',') -eq 'IDE0007,IDE0008') 'The cleanup-fixed rules are the non-empty lines of the file'

  Write-Host 'Stop hook reads SARIF'
  $repo = New-TestRepo 'hook-sarif'
  $sarifObj = Join-Path $repo 'artifacts/claude-verify/obj/App'
  New-Item -ItemType Directory -Force $sarifObj | Out-Null
  $repoUri = $repo.Replace('\', '/')
  $sarifResult = {
    param($rule, $file, $line)
    @{ ruleId = $rule; level = 'warning'; message = @{ text = "Message of $rule" }; locations = @(@{ physicalLocation = @{
          artifactLocation = @{ uri = "file:///$repoUri/backend/App/$file" }; region = @{ startLine = $line; startColumn = 5 } } }) }
  }
  @{ version = '2.1.0'; runs = @(@{ results = @(
          (& $sarifResult 'CS0219' 'A.cs' 3), (& $sarifResult 'CS0168' 'Z.cs' 4)) }) } |
    ConvertTo-Json -Depth 10 | Set-Content (Join-Path $sarifObj 'App.sarif')
  $env:FAKE_DOTNET_BUILD = '1'
  $result = Invoke-Hook $repo
  Remove-Item Env:FAKE_DOTNET_BUILD
  Assert-That ($result.ExitCode -eq 2 -and $result.Output.Contains('backend/App/A.cs(3,5): warning CS0219: Message of CS0219')) `
    "The hook reports the SARIF warning of a changed file when the build compiles nothing (output: $($result.Output))"
  Assert-That (-not $result.Output.Contains('Z.cs')) 'The hook reports no SARIF warning of an unchanged file'

  $repo = New-TestRepo 'hook-sarif-failed'
  $sarifObj = Join-Path $repo 'artifacts/claude-verify/obj/App'
  New-Item -ItemType Directory -Force $sarifObj | Out-Null
  $repoUri = $repo.Replace('\', '/')
  $failedResult = {
    param($rule, $level, $file, $line)
    @{ ruleId = $rule; level = $level; message = @{ text = "Message of $rule" }; locations = @(@{ physicalLocation = @{
          artifactLocation = @{ uri = "file:///$repoUri/backend/App/$file" }; region = @{ startLine = $line; startColumn = 5 } } }) }
  }
  @{ version = '2.1.0'; runs = @(@{ results = @(
          (& $failedResult 'CS0219' 'warning' 'A.cs' 3), (& $failedResult 'CS0168' 'warning' 'Z.cs' 4),
          (& $failedResult 'CS0103' 'error' 'Z.cs' 9)) }) } |
    ConvertTo-Json -Depth 10 | Set-Content (Join-Path $sarifObj 'App.sarif')
  $env:FAKE_DOTNET_BUILD = 'fail'
  $result = Invoke-Hook $repo
  Remove-Item Env:FAKE_DOTNET_BUILD
  Assert-That ($result.ExitCode -eq 2 -and $result.Output.Contains('backend/App/A.cs(3,5): warning CS0219: Message of CS0219') -and
    $result.Output.Contains('backend/App/Z.cs(9,5): error CS0103: Message of CS0103')) `
    "A failed build reports the warning of a changed file and the error of an unchanged file (output: $($result.Output))"
  Assert-That (-not $result.Output.Contains('CS0168')) 'A failed build reports no warning of an unchanged file'

  Write-Host 'Stop hook build text'
  $nuText = "$work\hook-text\backend\App\App.csproj : warning NU1903: Package 'X' 1.0 has a known vulnerability. [x.csproj]"
  $repo = New-TestRepo 'hook-text-warning'
  Add-Content (Join-Path $repo 'backend/App/App.csproj') '<!-- edited -->'
  $env:FAKE_DOTNET_BUILD = '1'
  $env:FAKE_DOTNET_TEXT = $nuText
  $result = Invoke-Hook $repo
  Remove-Item Env:FAKE_DOTNET_BUILD, Env:FAKE_DOTNET_TEXT
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output.Contains('NU1903')) `
    "An NU1903 build text warning does not block the stop, also when a .csproj changed (output: $($result.Output))"

  $repo = New-TestRepo 'hook-text-error'
  $textSarifObj = Join-Path $repo 'artifacts/claude-verify/obj/App'
  New-Item -ItemType Directory -Force $textSarifObj | Out-Null
  $repoUri = $repo.Replace('\', '/')
  @{ version = '2.1.0'; runs = @(@{ results = @(& $sarifResult 'CS0219' 'A.cs' 3) }) } |
    ConvertTo-Json -Depth 10 | Set-Content (Join-Path $textSarifObj 'App.sarif')
  $env:FAKE_DOTNET_BUILD = 'fail'
  $env:FAKE_DOTNET_TEXT = 'D:\x\Directory.Build.targets(3,5): error MSB4057: The target "X" does not exist in the project. [y.csproj]'
  $result = Invoke-Hook $repo
  Remove-Item Env:FAKE_DOTNET_BUILD, Env:FAKE_DOTNET_TEXT
  Assert-That ($result.ExitCode -eq 2 -and $result.Output.Contains('error MSB4057')) `
    "An MSB error of a failed build is reported (output: $($result.Output))"

  $repo = New-TestRepo 'hook-targets'
  git -C $repo checkout -q -- backend 2>&1 | Out-Null
  '<Project />' | Set-Content (Join-Path $repo 'backend/Directory.Build.targets')
  $env:FAKE_DOTNET_BUILD = 'fail'
  $env:FAKE_DOTNET_TEXT = 'D:\x\y.targets(1,1): error MSB4025: The project could not be loaded. [y.csproj]'
  $result = Invoke-Hook $repo
  Remove-Item Env:FAKE_DOTNET_BUILD, Env:FAKE_DOTNET_TEXT
  Assert-That ($result.ExitCode -eq 2 -and $result.Output.Contains('MSB4025')) `
    "A changed .targets file starts the build (output: $($result.Output))"

  Write-Host 'Stale turn trees'
  $repo = New-TestRepo 'stale-turns'
  $turnOld = Get-TurnTreePath $repo 'ended-session'
  $turnRecent = Get-TurnTreePath $repo 'recent-session'
  New-Item -ItemType Directory -Force (Split-Path $turnOld) | Out-Null
  foreach ($path in $turnOld, $turnRecent) { Set-Content $path 'x' }
  (Get-Item $turnOld).LastWriteTimeUtc = [DateTime]::UtcNow.AddDays(-2)
  (Get-Item $turnRecent).LastWriteTimeUtc = [DateTime]::UtcNow.AddHours(-23)
  [void](Invoke-Hook $repo 'new-session' $HooksDir 'record-turn-start.ps1')
  Assert-That (-not (Test-Path $turnOld)) 'A turn tree file older than 1 day is deleted at the start of a turn'
  Assert-That ((Test-Path $turnRecent) -and (Test-Path (Get-TurnTreePath $repo 'new-session'))) 'A younger turn tree file and the new one stay'
}
finally {
  Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
  Write-Host "$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host 'All checks passed.'
exit 0
