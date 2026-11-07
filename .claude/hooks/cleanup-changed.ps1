#Requires -Version 7
# Runs JetBrains ReSharper code cleanup on the given C# files (repository-relative paths).
# Without -Files, it cleans every backend C# file changed since HEAD (manual use only).
# verify-changed.ps1 calls it from the Stop and SubagentStop hooks with the files that git reports as changed in the
# checkout (main or worktree) and with that checkout. A manual run without -Repo cleans the checkout of the current
# directory.
#
# Safety against other writers (other sessions edit the same checkout):
# - One cleanup per checkout at a time: a named mutex (the same one that verify-changed.ps1 holds, so it is re-entrant).
# - The cleanup runs on a private copy of backend/ (inside the .git directory), never on the live files. The result
#   replaces a live file only when that file still has the content hash that the script read before the copy. A file
#   that changed during the run keeps the content of its other writer. A file that no longer exists is never
#   created again. The script writes one output line for each file that it skipped.

[CmdletBinding()]
param(
  [string[]] $Files,
  [string] $Repo
)

$ErrorActionPreference = 'Stop'

$repo = if ($Repo) { $Repo } else { git rev-parse --show-toplevel 2>$null }
if (-not $repo) {
  $repo = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { $PSScriptRoot | Split-Path | Split-Path }
}
$repo = [IO.Path]::GetFullPath($repo).TrimEnd([char]92, [char]47)
$solution = Join-Path $repo 'backend/HealthyFac.slnx'
$settings = Join-Path $repo 'backend/HealthyFac.sln.DotSettings'
if (-not (Test-Path $solution) -or -not (Test-Path $settings)) { exit 0 }

$jb = Get-Command jb -ErrorAction SilentlyContinue
if (-not $jb) {
  $candidate = Join-Path $env:USERPROFILE '.dotnet/tools/jb.exe'
  if (Test-Path $candidate) { $jb = $candidate } else {
    Write-Output 'Cleanup skipped: the jb tool is not installed (dotnet tool install JetBrains.ReSharper.GlobalTools -g).'
    exit 0
  }
} else {
  $jb = $jb.Source
}

function Get-FileHashText([string] $path) {
  [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($path)))
}

$repoHash = [Convert]::ToHexString(
  [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repo.ToLowerInvariant())))
$lock = [Threading.Mutex]::new($false, "Local\HealthyFac-verify-$($repoHash.Substring(0, 16))")
Write-Verbose 'Waiting for the checkout lock.'
$hasLock = try { $lock.WaitOne([TimeSpan]::FromSeconds(480)) } catch [Threading.AbandonedMutexException] { $true }
if (-not $hasLock) {
  $lock.Dispose()
  Write-Output 'Cleanup skipped: another run held the lock for 480 seconds.'
  exit 0
}

$shadow = $null
Push-Location $repo
try {
  if (-not $Files) {
    $tracked = @(git diff --name-only HEAD -- '*.cs' 2>$null)
    $untracked = @(git ls-files -o --exclude-standard -- '*.cs' 2>$null)
    $Files = @($tracked + $untracked)
  }

  $targets = @($Files) |
    Where-Object { $_ -like 'backend/*.cs' } |
    Sort-Object -Unique |
    Where-Object { Test-Path (Join-Path $repo $_) }

  if ($targets.Count -eq 0) { exit 0 }

  $before = @{}
  foreach ($target in $targets) { $before[$target] = Get-FileHashText (Join-Path $repo $target) }

  $shadowParent = [IO.Path]::GetFullPath((git rev-parse --git-path claude-cleanup-shadow), $repo)
  $shadow = Join-Path $shadowParent ([Guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force $shadow | Out-Null
  & robocopy (Join-Path $repo 'backend') (Join-Path $shadow 'backend') /E /XD bin obj artifacts node_modules .vs `
      /NFL /NDL /NJH /NJS /NP | Out-Null
  if ($LASTEXITCODE -ge 8) {
    Write-Output "Cleanup skipped: robocopy of backend/ into the private copy failed (exit code $LASTEXITCODE)."
    exit 0
  }
  $editorConfig = Join-Path $repo '.editorconfig'
  if (Test-Path $editorConfig) { Copy-Item $editorConfig (Join-Path $shadow '.editorconfig') }
  $shadowSolution = Join-Path $shadow 'backend/HealthyFac.slnx'
  $shadowSettings = Join-Path $shadow 'backend/HealthyFac.sln.DotSettings'

  # "Optimize using directives" removes each using that it cannot resolve. Without restored packages (a new
  # checkout, or a package added this turn) it removes valid usings such as Microsoft.EntityFrameworkCore, so the
  # cleanup runs only after a successful restore. A failed restore skips the cleanup; the build then reports it.
  $restoreOutput = @(& dotnet restore $shadowSolution --verbosity quiet 2>&1 | ForEach-Object { "$_" })
  if ($LASTEXITCODE -ne 0) {
    Write-Output "Cleanup skipped: dotnet restore of the private copy failed: $($restoreOutput | Select-Object -Last 3)"
    exit 0
  }

  # A target whose copy differs from the hash taken before the copy changed in between: it is skipped below.
  $copied = @{}
  foreach ($target in $targets) { $copied[$target] = Get-FileHashText (Join-Path $shadow $target) }

  # --include paths are relative to the solution directory (backend/).
  $include = (@($targets) | ForEach-Object { $_ -replace '^backend/', '' }) -join ';'

  # The "HealthyFac" profile (backend/HealthyFac.sln.DotSettings, also the Rider team-shared layer) holds the tasks
  # of "Built-in: Reformat & Apply Syntax Style" plus "Optimize using directives": it applies the layout and the
  # .editorconfig syntax style (braces, expression bodies, trailing commas, `var`, type keywords, no `this.`), sorts
  # the using directives (System first), and removes the unused ones. Do not use the default Full Cleanup profile:
  # it drops `private set`, reorders members and strips XML doc indents. --settings names the file explicitly, so
  # the run does not depend on how JetBrains maps HealthyFac.slnx to its settings file.
  & $jb cleanupcode --no-build --verbosity=WARN --settings="$shadowSettings" --profile="HealthyFac" `
      --include="$include" $shadowSolution 2>&1 |
    Select-Object -Last 5

  foreach ($target in $targets) {
    $live = Join-Path $repo $target
    if (-not (Test-Path -LiteralPath $live)) {
      Write-Output "Cleanup result dropped for ${target}: the file was deleted during the cleanup."
      continue
    }
    $result = Join-Path $shadow $target
    if ((Get-FileHashText $result) -eq $copied[$target]) { continue }
    if ($copied[$target] -ne $before[$target] -or (Get-FileHashText $live) -ne $before[$target]) {
      Write-Output "Cleanup result dropped for ${target}: another writer changed the file during the cleanup."
      continue
    }
    [IO.File]::WriteAllBytes($live, [IO.File]::ReadAllBytes($result))
  }
}
finally {
  Pop-Location
  if ($shadow -and (Test-Path -LiteralPath $shadow)) {
    Remove-Item -LiteralPath $shadow -Recurse -Force -ErrorAction SilentlyContinue
  }
  $lock.ReleaseMutex()
  $lock.Dispose()
}
exit 0
