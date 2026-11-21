#Requires -Version 7
# UserPromptSubmit hook: stores the tree ID of the working tree at the start of a turn (Get-TreeId of
# checkout-helpers.ps1), by session_id, in the git directory of the checkout that holds this script file (as
# verify-changed.ps1 resolves it; never from the hook input cwd). verify-changed.ps1 compares it at Stop: when the tree
# is equal, the turn changed no file, and the Stop hook does nothing. Git only, no transcript. It prints nothing and
# never blocks.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'checkout-helpers.ps1')

$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
if (-not $hookInput.session_id) { exit 0 }
$repo = git -C $PSScriptRoot rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0 -or -not $repo) { exit 0 }

$turnStateMaxAge = [TimeSpan]::FromDays(1)
Remove-StaleTurnStates $repo $turnStateMaxAge

$tree = try { Get-TreeId -Repo $repo } catch { $null }
if (-not $tree) { exit 0 }
$path = Get-TurnTreePath $repo $hookInput.session_id
New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
Set-Content -LiteralPath $path -Value $tree -Encoding utf8NoBOM
exit 0
