#Requires -Version 7
# UserPromptSubmit hook: stores the fingerprint of the working tree at the start of a turn, by session_id, in the git
# directory of the checkout that holds this script file (as verify-changed.ps1 resolves it; never from the hook
# input cwd). verify-changed.ps1 compares it at Stop: when the working
# tree is equal, the turn changed no file, and the Stop hook does nothing. Git only, no transcript. It prints nothing
# and never blocks.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'worktree-fingerprint.ps1')

$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
if (-not $hookInput.session_id) { exit 0 }
$repo = git -C $PSScriptRoot rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0 -or -not $repo) { exit 0 }

$turnFingerprintMaxAge = [TimeSpan]::FromDays(1)
Remove-StaleTurnFingerprints $repo $turnFingerprintMaxAge

$path = Get-TurnFingerprintPath $repo $hookInput.session_id
New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
Set-Content -LiteralPath $path -Value (Get-WorktreeFingerprint $repo) -Encoding utf8NoBOM
exit 0
