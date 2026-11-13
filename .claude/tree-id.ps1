#Requires -Version 7
# tree-id.ps1 - prints one git tree ID for the working tree of a checkout: every tracked and every untracked file that
# .gitignore does not exclude. Two equal IDs mean the same content, so the reviewer can tell that the code is the code
# that the executor tested (.claude/commands/implement.md, review.md).
# -Area limits the tree to one test area, so a change in one area keeps the full run of the other area valid:
#   frontend  the folders frontend/ and keycloak-theme/;
#   backend   every other path;
#   all       the whole tree (default).
# -Ref reads the tree of a commit instead of the working tree (e.g., the base of the feature).
# It works on a copy of the index (GIT_INDEX_FILE), so the real index, every ref, and every file stay as they are.
# The written blobs and trees stay as loose objects; `git gc` prunes them after 2 weeks.
# Usage: pwsh .claude/tree-id.ps1 [-Repo <path>] [-Area all|backend|frontend] [-Ref <commit>]
# Without -Repo, it reads the checkout that holds this script file, not the current folder.
# Exit code 1 when git fails.
param(
  [string]$Repo = (git -C $PSScriptRoot rev-parse --show-toplevel),
  [ValidateSet('all', 'backend', 'frontend')][string]$Area = 'all',
  [string]$Ref
)

$ErrorActionPreference = 'Stop'
$FrontendPaths = @('frontend', 'keycloak-theme')

$indexPath = (git -C $Repo rev-parse --path-format=absolute --git-path index).Trim()
if ($LASTEXITCODE -ne 0) { Write-Error "No git checkout: $Repo"; exit 1 }

$temporaryIndex = Join-Path ([IO.Path]::GetTempPath()) "hf-tree-id-$([Guid]::NewGuid().ToString('N')).idx"
$previousIndex = $env:GIT_INDEX_FILE
try {
  $env:GIT_INDEX_FILE = $temporaryIndex
  if ($Ref) {
    git -C $Repo read-tree $Ref | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "git read-tree $Ref failed."; exit 1 }
  } else {
    if (Test-Path -LiteralPath $indexPath) { Copy-Item -LiteralPath $indexPath -Destination $temporaryIndex }
    git -C $Repo add -A | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error 'git add failed.'; exit 1 }
  }
  $excluded = switch ($Area) {
    'backend' { $FrontendPaths }
    'frontend' { @('.') + @($FrontendPaths | ForEach-Object { ":(exclude)$_" }) }
    default { @() }
  }
  if ($excluded.Count -gt 0) {
    git -C $Repo rm -r -q --cached --ignore-unmatch -- @excluded | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error 'git rm --cached failed.'; exit 1 }
  }
  $tree = (git -C $Repo write-tree).Trim()
  if ($LASTEXITCODE -ne 0) { Write-Error 'git write-tree failed.'; exit 1 }
  $tree
} finally {
  if ($null -eq $previousIndex) { Remove-Item Env:GIT_INDEX_FILE -ErrorAction SilentlyContinue } else { $env:GIT_INDEX_FILE = $previousIndex }
  Remove-Item -LiteralPath $temporaryIndex, "$temporaryIndex.lock" -Force -ErrorAction SilentlyContinue
}
