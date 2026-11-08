#Requires -Version 7
# tree-id.ps1 - prints one git tree ID for the whole working tree of a checkout: every tracked and every untracked
# file that .gitignore does not exclude. Two equal IDs mean the same content, so the reviewer can tell that the code
# is the code that the executor tested (.claude/commands/implement.md, review.md).
# It works on a copy of the index (GIT_INDEX_FILE), so the real index, every ref, and every file stay as they are.
# The written blobs and trees stay as loose objects; `git gc` prunes them after 2 weeks.
# Usage: pwsh .claude/tree-id.ps1 [-Repo <path>]
# Exit code 1 when git fails.
param([string]$Repo = (Get-Location).Path)

$ErrorActionPreference = 'Stop'

$indexPath = (git -C $Repo rev-parse --path-format=absolute --git-path index).Trim()
if ($LASTEXITCODE -ne 0) { Write-Error "No git checkout: $Repo"; exit 1 }

$temporaryIndex = Join-Path ([IO.Path]::GetTempPath()) "hf-tree-id-$([Guid]::NewGuid().ToString('N')).idx"
$previousIndex = $env:GIT_INDEX_FILE
try {
  if (Test-Path -LiteralPath $indexPath) { Copy-Item -LiteralPath $indexPath -Destination $temporaryIndex }
  $env:GIT_INDEX_FILE = $temporaryIndex
  git -C $Repo add -A | Out-Null
  if ($LASTEXITCODE -ne 0) { Write-Error 'git add failed.'; exit 1 }
  $tree = (git -C $Repo write-tree).Trim()
  if ($LASTEXITCODE -ne 0) { Write-Error 'git write-tree failed.'; exit 1 }
  $tree
} finally {
  if ($null -eq $previousIndex) { Remove-Item Env:GIT_INDEX_FILE -ErrorAction SilentlyContinue } else { $env:GIT_INDEX_FILE = $previousIndex }
  Remove-Item -LiteralPath $temporaryIndex, "$temporaryIndex.lock" -Force -ErrorAction SilentlyContinue
}
