#Requires -Version 7
# Stop hook: formats the backend files touched this turn, builds the solution, and reports build warnings and
# errors in those files back to Claude. Exit code 2 blocks the stop and sends stderr to Claude.
# A file counts as touched when its content hash differs from the hash saved at the previous stop. The hash
# state lives in the .git directory, so edits made outside Edit/Write (e.g. sed) count too, and uncommitted
# changes from earlier turns do not trigger a rebuild.
# The hook blocks once per stop attempt. When stop_hook_active is set, it reports and lets the turn end,
# so a warning Claude cannot fix does not loop. Files with diagnostics stay out of the saved state,
# so the next stop checks them again.

$ErrorActionPreference = 'Stop'

$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$repo = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { $PSScriptRoot | Split-Path | Split-Path }
$solution = Join-Path $repo 'backend/HealthyFac.slnx'
if (-not (Test-Path $solution)) { exit 0 }

function Get-ChangedBackendFiles {
  $patterns = '*.cs', '*.csproj', '*.props'
  $tracked = @(git diff --name-only HEAD -- $patterns 2>$null)
  $untracked = @(git ls-files -o --exclude-standard -- $patterns 2>$null)

  @($tracked + $untracked) |
    Where-Object { $_ -like 'backend/*' } |
    Sort-Object -Unique |
    Where-Object { Test-Path (Join-Path $repo $_) }
}

function Get-ContentHashes([string[]] $files) {
  $hashes = @{}
  if ($files.Count -eq 0) { return $hashes }
  $values = @(git hash-object -- $files)
  for ($i = 0; $i -lt $files.Count; $i++) { $hashes[$files[$i]] = $values[$i] }
  $hashes
}

Push-Location $repo
try {
  $statePath = Join-Path $repo (git rev-parse --git-path claude-verify-state.json)
  $saved = @{}
  if (Test-Path $statePath) {
    (Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable).GetEnumerator() |
      ForEach-Object { $saved[$_.Key] = $_.Value }
  }

  $files = @(Get-ChangedBackendFiles)
  $current = Get-ContentHashes $files

  $touched = @($files | Where-Object { $saved[$_] -ne $current[$_] })
  $deleted = @($saved.Keys | Where-Object { -not (Test-Path (Join-Path $repo $_)) })

  if ($touched.Count -eq 0 -and $deleted.Count -eq 0) { exit 0 }

  $touchedCode = @($touched | Where-Object { $_ -like '*.cs' })
  if ($touchedCode.Count -gt 0) {
    & (Join-Path $PSScriptRoot 'cleanup-changed.ps1') -Files $touchedCode | Out-Null
  }

  $output = & dotnet build $solution -nologo -clp:NoSummary 2>&1 | ForEach-Object { "$_" }
  $buildFailed = $LASTEXITCODE -ne 0

  $touchedPaths = $touched | ForEach-Object { (Join-Path $repo $_).Replace('/', '\').ToLowerInvariant() }
  $flagged = [Collections.Generic.HashSet[string]]::new()

  $diagnostics = $output |
    Where-Object { $_ -match '^(?<file>.+?)\(\d+,\d+\): (warning|error) ' } |
    Where-Object { $buildFailed -or $touchedPaths -contains $Matches.file.ToLowerInvariant() } |
    ForEach-Object {
      $line = ($_ -replace ' \[[^\]]+\]$', '').Replace("$repo\", '')
      [void]$flagged.Add(($line -replace '\(\d+,\d+\): .*$', '').Replace('\', '/'))
      $line
    } |
    Sort-Object -Unique

  $state = Get-ContentHashes @(Get-ChangedBackendFiles)
  if ($buildFailed) { $state = @{} }
  foreach ($file in $flagged) { $state.Remove($file) }
  New-Item -ItemType Directory -Force (Split-Path $statePath) | Out-Null
  $state | ConvertTo-Json -Compress | Set-Content $statePath -Encoding utf8NoBOM

  if (-not $buildFailed -and $diagnostics.Count -eq 0) { exit 0 }
  if ($diagnostics.Count -eq 0) { $diagnostics = $output | Select-Object -Last 20 }

  $report = @(
    'The build reports problems in the backend files touched this turn. Fix every item before you finish.'
    $diagnostics
  ) -join [Environment]::NewLine

  if ($hookInput.stop_hook_active) {
    Write-Output "Verification still fails after one fix attempt:$([Environment]::NewLine)$report"
    exit 0
  }

  [Console]::Error.WriteLine($report)
  exit 2
}
finally {
  Pop-Location
}
