#Requires -Version 7
# Runs JetBrains ReSharper code cleanup on the given C# files (repository-relative paths).
# Without -Files, it cleans every backend C# file changed since HEAD.
# verify-changed.ps1 calls it from the Stop and SubagentStop hooks with the files touched this turn and the checkout
# (main or worktree) that holds them. A manual run without -Repo cleans the checkout of the current directory.

param(
  [string[]] $Files,
  [string] $Repo
)

$ErrorActionPreference = 'Stop'

$repo = if ($Repo) { $Repo } else { git rev-parse --show-toplevel 2>$null }
if (-not $repo) {
  $repo = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { $PSScriptRoot | Split-Path | Split-Path }
}
$solution = Join-Path $repo 'backend/HealthyFac.slnx'
$settings = Join-Path $repo 'backend/HealthyFac.sln.DotSettings'
if (-not (Test-Path $solution) -or -not (Test-Path $settings)) { exit 0 }

$jb = Get-Command jb -ErrorAction SilentlyContinue
if (-not $jb) {
  $candidate = Join-Path $env:USERPROFILE '.dotnet/tools/jb.exe'
  if (Test-Path $candidate) { $jb = $candidate } else { exit 0 }
} else {
  $jb = $jb.Source
}

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

  # "Optimize using directives" removes each using that it cannot resolve. Without restored packages (a new
  # checkout, or a package added this turn) it removes valid usings such as Microsoft.EntityFrameworkCore, so the
  # cleanup runs only after a successful restore. A failed restore skips the cleanup; the build then reports it.
  & dotnet restore $solution --verbosity quiet 2>&1 | Out-Null
  if ($LASTEXITCODE -ne 0) { exit 0 }

  # --include paths are relative to the solution directory (backend/).
  $include = (@($targets) | ForEach-Object { $_ -replace '^backend/', '' }) -join ';'

  # The "HealthyFac" profile (backend/HealthyFac.sln.DotSettings, also the Rider team-shared layer) holds the tasks
  # of "Built-in: Reformat & Apply Syntax Style" plus "Optimize using directives": it applies the layout and the
  # .editorconfig syntax style (braces, expression bodies, trailing commas, `var`, type keywords, no `this.`), sorts
  # the using directives (System first), and removes the unused ones. Do not use the default Full Cleanup profile:
  # it drops `private set`, reorders members and strips XML doc indents. --settings names the file explicitly, so
  # the run does not depend on how JetBrains maps HealthyFac.slnx to its settings file.
  & $jb cleanupcode --no-build --verbosity=WARN --settings="$settings" --profile="HealthyFac" `
      --include="$include" $solution 2>&1 |
    Select-Object -Last 5
}
finally {
  Pop-Location
}
exit 0
