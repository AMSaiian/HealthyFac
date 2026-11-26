#Requires -Version 7
# format-changed.ps1 - runs the formatters of the repo on the given files (repository-relative paths), as the Stop hook
# does: the JetBrains cleanup for C# files (hooks/cleanup-changed.ps1) and Prettier for the files of the web projects.
# run-area.ps1 and run-tests.ps1 call it before a run, so the tree of the run is the tree after the format step.
# The script formats every given file on each call.
# Output: the notes of a skipped or dropped cleanup ("Cleanup ..."), one per line. A web project without node_modules
# is skipped without a note, as in the hook.
# Usage: pwsh .claude/format-changed.ps1 -Repo <path> -Files <path>[,<path>...]
param(
  [Parameter(Mandatory = $true)][string]$Repo,
  [string[]]$Files = @()
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'hooks/command-line-batches.ps1')
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$WebProjects = @('frontend', 'keycloak-theme')
$WebPattern = '\.(ts|html|css|json|webmanifest)$'

$Files = @($Files | ForEach-Object { $_ -split ',' } | Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } |
    Sort-Object -Unique)

$code = @($Files | Where-Object { $_ -like '*.cs' })
if ($code.Count -gt 0) {
  @(& (Join-Path $PSScriptRoot 'hooks/cleanup-changed.ps1') -Files $code -Repo $Repo | Where-Object { $_ -like 'Cleanup *' })
}

foreach ($project in $WebProjects) {
  $web = @($Files | Where-Object { $_ -like "$project/*" -and $_ -match $WebPattern -and $_ -notlike '*/package-lock.json' })
  $folder = Join-Path $Repo $project
  if ($web.Count -eq 0 -or -not (Test-Path (Join-Path $folder 'node_modules'))) { continue }
  Push-Location $folder
  try {
    $relative = @($web | ForEach-Object { $_.Substring($project.Length + 1) })
    foreach ($batch in Split-ArgumentBatches $relative (Get-ArgumentBudget 'npx.cmd')) {
      & npx prettier --write --ignore-unknown @batch 2>&1 | Out-Null
    }
  }
  finally { Pop-Location }
}
