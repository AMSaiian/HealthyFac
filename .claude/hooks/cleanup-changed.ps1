#Requires -Version 7
# Runs JetBrains ReSharper code cleanup on the given C# files (repository-relative paths).
# Without -Files, it cleans every backend C# file changed since HEAD.
# verify-changed.ps1 calls it from the Stop hook with the files touched this turn.

param(
  [string[]] $Files
)

$ErrorActionPreference = 'Stop'

$repo = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { $PSScriptRoot | Split-Path | Split-Path }
$solution = Join-Path $repo 'backend/HealthyFac.slnx'
if (-not (Test-Path $solution)) { exit 0 }

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

  # --include paths are relative to the solution directory (backend/).
  $include = (@($targets) | ForEach-Object { $_ -replace '^backend/', '' }) -join ';'

  # "Built-in: Reformat Code" is formatting only. The default Full Cleanup profile
  # rewrites code: it drops `private set`, reorders members and strips XML doc indents.
  & $jb cleanupcode --no-build --verbosity=WARN --profile="Built-in: Reformat Code" `
      --include="$include" $solution 2>&1 |
    Select-Object -Last 5
}
finally {
  Pop-Location
}
exit 0
