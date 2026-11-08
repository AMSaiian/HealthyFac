#Requires -Version 7
# resume-feature.ps1 - reopens the three named Claude Code sessions of one feature that start-feature.ps1 created.
# Each tab resumes its session by name (claude --resume '<feature>-<role>'), so the conversation and its role continue.
# Each tab opens with the Windows Terminal profile -TerminalProfile (name or GUID), so it uses that profile font.
# Each tab runs pwsh with the user profile (no -NoProfile), so the prompt setup loads, and stays open after claude exits.
# Usage, from anywhere inside the repo:  resume-feature.ps1 late-fees
param(
  [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Feature,
  [string]$TerminalProfile = 'PowerShell'
)

$repo = git rev-parse --show-toplevel 2>$null
if (-not $repo) { Write-Error "Run this from inside a git repo."; exit 1 }
$repo = $repo -replace '/', '\'

function New-ClaudeCommand([string]$Role, [string]$Model) {
  "claude --chrome --resume '$Feature-$Role' --model $Model"
}

$sessions = @(
  @{ Role = 'spec'; Model = 'opus' },
  @{ Role = 'executor'; Model = 'sonnet' },
  @{ Role = 'reviewer'; Model = 'opus' }
)

$wtArguments = @('-w', '0')
foreach ($session in $sessions) {
  if ($wtArguments.Count -gt 2) { $wtArguments += ';' }
  $wtArguments += @(
    'new-tab', '-p', $TerminalProfile, '-d', $repo, '--title', "$Feature $($session.Role)",
    'pwsh', '-NoExit', '-Command', (New-ClaudeCommand $session.Role $session.Model)
  )
}

wt @wtArguments
