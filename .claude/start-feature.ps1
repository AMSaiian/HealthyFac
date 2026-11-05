# start-feature.ps1 - opens three named Claude Code sessions for one feature, each with its
# role command already loaded. Executor and reviewer wait for a handoff before doing anything.
# Usage, from anywhere inside the repo:  start-feature.ps1 late-fees
param([Parameter(Mandatory = $true)][string]$Feature)

$repo = git rev-parse --show-toplevel 2>$null
if (-not $repo) { Write-Error "Run this from inside a git repo."; exit 1 }
$repo = $repo -replace '/', '\'

wt -w 0 `
  new-tab -d "$repo" --title "$Feature spec" claude --name "$Feature-spec" --model opus "/spec $Feature" `; `
  new-tab -d "$repo" --title "$Feature executor" claude --name "$Feature-executor" --model sonnet "/implement $Feature" `; `
  new-tab -d "$repo" --title "$Feature reviewer" claude --name "$Feature-reviewer" --model opus "/review $Feature"
