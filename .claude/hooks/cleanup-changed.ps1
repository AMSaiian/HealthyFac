#Requires -Version 7
# Runs JetBrains ReSharper code cleanup on C# files changed since HEAD.
# Wired to the Stop hook so it runs once per turn, not once per edit.

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
  $tracked = @(git diff --name-only HEAD -- '*.cs' 2>$null)
  $untracked = @(git ls-files -o --exclude-standard -- '*.cs' 2>$null)

  $files = @($tracked + $untracked) |
    Where-Object { $_ -like 'backend/*' } |
    Sort-Object -Unique |
    Where-Object { Test-Path (Join-Path $repo $_) }

  if ($files.Count -eq 0) { exit 0 }

  # --include paths are relative to the solution directory (backend/).
  $include = ($files | ForEach-Object { $_ -replace '^backend/', '' }) -join ';'

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
