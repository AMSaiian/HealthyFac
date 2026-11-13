#Requires -Version 7
# format-changed.ps1 - runs the formatters of the repo on the given files (repository-relative paths), as the Stop hook
# does: the JetBrains cleanup for C# files (hooks/cleanup-changed.ps1) and Prettier for the files of the web projects.
# check-handoff.ps1 calls it to find the files that the hook left unformatted (a skipped cleanup, a hook that gave up
# after one fix attempt, a file changed after the last stop).
# Output: the notes of a skipped or dropped cleanup ("Cleanup ..."), one per line. A web project without node_modules
# is skipped without a note, as in the hook.
# Usage: pwsh .claude/format-changed.ps1 -Repo <path> -Files <path>[,<path>...]
param(
  [Parameter(Mandatory = $true)][string]$Repo,
  [string[]]$Files = @()
)

$ErrorActionPreference = 'Stop'
$WebProjects = @('frontend', 'keycloak-theme')
$WebPattern = '\.(ts|html|css|json|webmanifest)$'

$code = @($Files | Where-Object { $_ -like '*.cs' })
if ($code.Count -gt 0) {
  & (Join-Path $PSScriptRoot 'hooks/cleanup-changed.ps1') -Files $code -Repo $Repo | Where-Object { $_ -like 'Cleanup *' }
}

foreach ($project in $WebProjects) {
  $web = @($Files | Where-Object { $_ -like "$project/*" -and $_ -match $WebPattern -and $_ -notlike '*/package-lock.json' })
  $folder = Join-Path $Repo $project
  if ($web.Count -eq 0 -or -not (Test-Path (Join-Path $folder 'node_modules'))) { continue }
  Push-Location $folder
  try { & npx prettier --write --ignore-unknown @($web | ForEach-Object { $_.Substring($project.Length + 1) }) 2>&1 | Out-Null }
  finally { Pop-Location }
}
