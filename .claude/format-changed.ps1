#Requires -Version 7
# format-changed.ps1 - runs the formatters of the repo on the given files (repository-relative paths), as the Stop hook
# does: the JetBrains cleanup for C# files (hooks/cleanup-changed.ps1) and Prettier for the files of the web projects.
# run-area.ps1 calls it before a full run, and check-handoff.ps1 calls it to find the files that are still unformatted
# (a skipped cleanup, a hook that gave up after one fix attempt, a file changed after the last stop).
# Skip: the state file claude-format-state.json in the git directory of the checkout holds the tree ID after the last
# run (Get-TreeId of hooks/checkout-helpers.ps1) and the files that the script formatted at that tree. When the
# current tree equals it, the script formats only the given files that are not in the list, so a second call after
# run-area.ps1 starts no JetBrains cleanup (about 30 seconds). A C# file stays out of the list when the run printed a
# "Cleanup" note.
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
$statePath = Get-GitStatePath $Repo 'claude-format-state.json'
$state = if (Test-Path -LiteralPath $statePath) {
  try { Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json } catch { $null }
}
$treeBefore = Get-TreeId -Repo $Repo
$formatted = if ($state -and $state.Tree -eq $treeBefore) { @($state.Files) } else { @() }
$todo = @($Files | Where-Object { $_ -notin $formatted })

$code = @($todo | Where-Object { $_ -like '*.cs' })
$notes = @()
if ($code.Count -gt 0) {
  $notes = @(& (Join-Path $PSScriptRoot 'hooks/cleanup-changed.ps1') -Files $code -Repo $Repo | Where-Object { $_ -like 'Cleanup *' })
  $notes
}

foreach ($project in $WebProjects) {
  $web = @($todo | Where-Object { $_ -like "$project/*" -and $_ -match $WebPattern -and $_ -notlike '*/package-lock.json' })
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

$done = @($todo | Where-Object { $notes.Count -eq 0 -or $_ -notlike '*.cs' })
$treeAfter = Get-TreeId -Repo $Repo
New-Item -ItemType Directory -Force (Split-Path $statePath) | Out-Null
[pscustomobject]@{ Tree = $treeAfter; Files = @(@($formatted) + $done | Sort-Object -Unique) } |
  ConvertTo-Json -Compress | Set-Content -LiteralPath $statePath -Encoding utf8NoBOM
