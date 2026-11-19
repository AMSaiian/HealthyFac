#Requires -Version 7
# PreToolUse hook of the reviewer subagent (.claude/agents/reviewer.md, frontmatter): the reviewer writes only
# review.md in the feature folder, never a repo file. The hook allows an Edit, a Write, or a NotebookEdit only on a
# file named review.md outside every checkout of this repository (the main one and its worktrees share one git common
# dir). It denies every other target with exit code 2, and the reason goes to the reviewer. A shell write is out of
# its reach; reviewer.md forbids it.
# The checkout test uses git from the nearest existing folder of the target, so a new file in an existing folder works.

$ErrorActionPreference = 'Stop'
$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$target = $hookInput.tool_input.file_path ?? $hookInput.tool_input.notebook_path
if (-not $target) {
  [Console]::Error.WriteLine('The reviewer writes only review.md in the feature folder; this call names no file.')
  exit 2
}

$path = [IO.Path]::GetFullPath($target)
$repoCommonDir = git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir 2>$null
$folder = Split-Path $path -Parent
while ($folder -and -not (Test-Path -LiteralPath $folder -PathType Container)) { $folder = Split-Path $folder -Parent }
$targetCommonDir = if ($folder) { git -C $folder rev-parse --path-format=absolute --git-common-dir 2>$null }
$isRepoFile = $targetCommonDir -and $repoCommonDir -and
  ([IO.Path]::GetFullPath($targetCommonDir).TrimEnd('\', '/') -eq [IO.Path]::GetFullPath($repoCommonDir).TrimEnd('\', '/'))
$isReviewFile = (Split-Path $path -Leaf) -eq 'review.md'

if ($isReviewFile -and -not $isRepoFile) { exit 0 }
[Console]::Error.WriteLine("The reviewer writes only review.md in the feature folder, never a repo file: $path")
exit 2
