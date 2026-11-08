#Requires -Version 7
# Deterministic check of the Stop hook: the turn skip, the checkout resolution, the lock, and the guard against other
# writers (CLAUDE.md, Verification Hook). It builds throw-away git repositories in a temp folder, replaces the
# JetBrains tool with a fake `jb` that appends "// CLEANED" to every file it is asked to clean, and runs the real hooks.
#
# Cases:
# - Turn skip: a turn that changes no file does nothing; a turn that changes a .cs file runs the cleanup.
# - Checkout: the Stop with cwd in checkout 1 cleans the changed files of checkout 1 and leaves checkout 2 unchanged.
# - Outside a checkout: a cwd outside any checkout does nothing, and the hook never falls back to another checkout.
# - Lost update: a file that another writer changes during the cleanup keeps that content.
# - Deleted file: a file deleted during the cleanup is not created again.
# - Skipped cleanup: a failed restore of the private copy prints a "Cleanup skipped" line and cleans nothing.
# - Frontend: the step formats changed files, including .webmanifest.
#   A lint line without a capital S reaches the output. A turn fingerprint file older than 1 day goes.
# - Lock: two cleanups of one checkout run one after the other; cleanups of two checkouts run at the same time.
#   The cases compare the order of logged events, never durations.
# - Proof: the turn skip case and the two guard cases run once more with the skip or the guard switched off in a copy
#   of the hook scripts. The checks must then show the difference, which proves that the skip or the guard causes it.
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
  "artifacts/`nbin/`nobj/" | Set-Content (Join-Path $repo '.gitignore')
  git -C $repo init -q 2>&1 | Out-Null
  git -C $repo config user.email t@t 2>&1 | Out-Null
  git -C $repo config user.name t 2>&1 | Out-Null
  git -C $repo config core.autocrlf false 2>&1 | Out-Null
  git -C $repo add -A 2>&1 | Out-Null
  git -C $repo commit -q -m base 2>&1 | Out-Null
  foreach ($file in 'A', 'B') { Add-Content (Join-Path $backend "App/$file.cs") '// edited' }
  $repo
}

function Invoke-Hook([string] $cwd, [string] $session = 'test', [string] $hooks = $HooksDir, [string] $script = 'verify-changed.ps1') {
  $inputJson = @{ session_id = $session; cwd = $cwd } | ConvertTo-Json -Compress
  $output = $inputJson | & pwsh -NoProfile -File (Join-Path $hooks $script) 2>&1
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
  Assert-That (Test-Cleaned $repo 'A') 'A first Stop without a stored fingerprint runs the cleanup'
  $result = Invoke-Hook $repo 's2'
  $count = ([regex]::Matches((Get-Content (Join-Path $repo 'backend/App/A.cs') -Raw), [regex]::Escape($marker))).Count
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output -and $count -eq 1) 'A Stop on a working tree that was verified already does nothing'

  $noSkip = New-ModifiedHooks 'noskip' 'verify-changed.ps1' `
    'if ($isUnchangedInTurn -or $isUnchangedSinceVerified) { exit 0 }' 'if ($false) { exit 0 }'
  $repo = New-TestRepo 'proof-turn'
  [void](Invoke-Hook $repo 's1' $HooksDir 'record-turn-start.ps1')
  [void](Invoke-Hook $repo 's1' $noSkip)
  Assert-That (Test-Cleaned $repo 'A') 'Proof: with the skip switched off, a turn without changes runs the cleanup'

  Write-Host 'Checkout resolved from cwd'
  $first = New-TestRepo 'checkout-one'
  $second = New-TestRepo 'checkout-two'
  [void](Invoke-Hook (Join-Path $first 'backend'))
  Assert-That ((Test-Cleaned $first 'A') -and (Test-Cleaned $first 'B')) 'Stop with cwd in checkout 1 cleans its changed files'
  Assert-That (-not (Test-Cleaned $second 'A') -and -not (Test-Cleaned $second 'B')) 'Stop with cwd in checkout 1 leaves checkout 2 unchanged'

  Write-Host 'cwd outside any checkout'
  $outside = Join-Path $work 'not-a-checkout'
  New-Item -ItemType Directory -Force $outside | Out-Null
  $env:CLAUDE_PROJECT_DIR = $second
  $result = Invoke-Hook $outside
  Remove-Item Env:CLAUDE_PROJECT_DIR
  Assert-That ($result.ExitCode -eq 0 -and -not $result.Output) 'Stop with cwd outside a checkout exits 0 and reports nothing'
  Assert-That (-not (Test-Cleaned $second 'A') -and -not (Test-Cleaned $second 'B')) 'Stop with cwd outside a checkout does not fall back to CLAUDE_PROJECT_DIR'

  $cleanup = Join-Path $HooksDir 'cleanup-changed.ps1'

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

  Write-Host 'Fingerprint of non-ASCII paths'
  . (Join-Path $HooksDir 'worktree-fingerprint.ps1')
  $repo = New-TestRepo 'fingerprint'
  $odd = Join-Path $repo "backend/App/$([char]0x00DC)ber.cs"
  [IO.File]::WriteAllText($odd, 'namespace Test; public class Uber { }')
  $fingerprintBefore = Get-WorktreeFingerprint $repo
  [IO.File]::WriteAllText($odd, 'namespace Test; public class Uber { public int X; }')
  $fingerprintAfter = Get-WorktreeFingerprint $repo
  Assert-That ($fingerprintBefore -ne $fingerprintAfter) 'An edit of an untracked file with a non-ASCII name changes the fingerprint'
  function Get-OldFingerprint([string] $repo) {
    $status = @(git -C $repo status --porcelain=v1 -uall 2>$null)
    $paths = @($status | ForEach-Object { $_.Substring(3).Trim('"') } | Where-Object { Test-Path -LiteralPath (Join-Path $repo $_) -PathType Leaf })
    $hashes = if ($paths.Count -gt 0) { @($paths | git -C $repo hash-object --stdin-paths 2>$null) } else { @() }
    (($status + $hashes) -join "`n")
  }
  [IO.File]::WriteAllText($odd, 'namespace Test; public class Uber { }')
  $oldBefore = Get-OldFingerprint $repo
  [IO.File]::WriteAllText($odd, 'namespace Test; public class Uber { public int X; }')
  $oldAfter = Get-OldFingerprint $repo
  Assert-That ($oldBefore -eq $oldAfter) 'Proof: the old parsing without -z keeps the fingerprint after the same edit'

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

  Write-Host 'Stale turn fingerprints'
  $repo = New-TestRepo 'stale-turns'
  $turnOld = Get-TurnFingerprintPath $repo 'ended-session'
  $turnRecent = Get-TurnFingerprintPath $repo 'recent-session'
  New-Item -ItemType Directory -Force (Split-Path $turnOld) | Out-Null
  foreach ($path in $turnOld, $turnRecent) { Set-Content $path 'x' }
  (Get-Item $turnOld).LastWriteTimeUtc = [DateTime]::UtcNow.AddDays(-2)
  (Get-Item $turnRecent).LastWriteTimeUtc = [DateTime]::UtcNow.AddHours(-23)
  [void](Invoke-Hook $repo 'new-session' $HooksDir 'record-turn-start.ps1')
  Assert-That (-not (Test-Path $turnOld)) 'A turn fingerprint file older than 1 day is deleted at the start of a turn'
  Assert-That ((Test-Path $turnRecent) -and (Test-Path (Get-TurnFingerprintPath $repo 'new-session'))) 'A younger turn fingerprint file and the new one stay'
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
