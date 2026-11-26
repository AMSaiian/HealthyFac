#Requires -Version 7
# tree-id.ps1 - prints one git tree ID for the working tree of a checkout: every tracked and every untracked file that
# .gitignore does not exclude. Two equal IDs mean the same content, so the reviewer can tell that the code is the code
# that the executor tested (.claude/agents/executor.md, reviewer.md). The logic is Get-TreeId in
# hooks/checkout-helpers.ps1 (the areas, -Ref, and the copy of the index are described there).
# Usage: pwsh .claude/tree-id.ps1 [-Repo <path>] [-Area all|backend|frontend|tooling] [-Ref <commit>]
# Without -Repo, it reads the checkout that holds this script file, not the current folder.
# Exit code 1 when git fails.
param(
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [ValidateSet('all', 'backend', 'frontend', 'tooling')][string]$Area = 'all',
  [string]$Ref
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')

try { Get-TreeId -Repo $Repo -Area $Area -Ref $Ref }
catch {
  [Console]::Error.WriteLine($_.Exception.Message)
  exit 1
}
