#Requires -Version 7
# start-feature.ps1 - opens three named Claude Code sessions for one feature, each with its
# role command already loaded. Executor and reviewer wait for a handoff before doing anything.
# Each tab opens with the Windows Terminal profile -TerminalProfile (name or GUID), so it uses that profile font.
# Each tab runs pwsh with the user profile (no -NoProfile), so the prompt setup loads, and stays open after claude exits.
# Usage, from anywhere inside the repo:  start-feature.ps1 late-fees
param(
  [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Feature,
  [string]$TerminalProfile = 'PowerShell'
)

$repo = git rev-parse --show-toplevel 2>$null
if (-not $repo) { Write-Error "Run this from inside a git repo."; exit 1 }
$repo = $repo -replace '/', '\'

function New-ClaudeCommand([string]$Role, [string]$Model, [string]$Command) {
  "claude --chrome --name '$Feature-$Role' --model $Model '/$Command $Feature'"
}

$sessions = @(
  @{ Role = 'spec'; Model = 'opus'; Command = 'spec' },
  @{ Role = 'executor'; Model = 'sonnet'; Command = 'implement' },
  @{ Role = 'reviewer'; Model = 'opus'; Command = 'review' }
)

$wtArguments = @('-w', '0')
foreach ($session in $sessions) {
  if ($wtArguments.Count -gt 2) { $wtArguments += ';' }
  $wtArguments += @(
    'new-tab', '-p', $TerminalProfile, '-d', $repo, '--title', "$Feature $($session.Role)",
    'pwsh', '-NoExit', '-Command', (New-ClaudeCommand $session.Role $session.Model $session.Command)
  )
}

wt @wtArguments
