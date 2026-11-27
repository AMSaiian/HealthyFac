#Requires -Version 7
# PreToolUse hook of the reviewer subagent (.claude/agents/reviewer.md, frontmatter): the reviewer writes only
# review.md in the feature folder, never a repo file, and scratch files in artifacts/claude-review/ of the checkout
# that holds this hook (git-ignored; $script:ReviewerScratchFolder of checkout-helpers.ps1). The hook allows an Edit, a
# Write, or a NotebookEdit only on a file named review.md outside every checkout of this repository (the main one and
# its worktrees share one git common dir), or on a path under that scratch folder (after GetFullPath, so ".." cannot
# leave it; case and slash direction do not matter). It denies every other target with exit code 2, and the reason
# goes to the reviewer. A shell write is out of its reach; reviewer-shell-scope.ps1 denies the known shell write forms.
# The checkout test uses git from the nearest existing folder of the target, so a new file in an existing folder works.
# Limit: no hook checks what a scratch script does when it runs, so a write inside it reaches any file; reviewer.md
# forbids it.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'checkout-helpers.ps1')
$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$target = $hookInput.tool_input.file_path ?? $hookInput.tool_input.notebook_path
if (-not $target) {
  [Console]::Error.WriteLine(
    "The reviewer writes only review.md in the feature folder and scratch files in $script:ReviewerScratchFolder/ of " +
    'the worktree; this call names no file.')
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

$checkoutRoot = (git -C $PSScriptRoot rev-parse --show-toplevel 2>$null)
$scratchFolder = if ($checkoutRoot) {
  ([IO.Path]::GetFullPath((Join-Path $checkoutRoot $script:ReviewerScratchFolder)) -replace '\\', '/').TrimEnd('/') + '/'
}
$isScratchFile = $scratchFolder -and
  ($path -replace '\\', '/').StartsWith($scratchFolder, [StringComparison]::OrdinalIgnoreCase)

if ($isScratchFile -or ($isReviewFile -and -not $isRepoFile)) { exit 0 }
[Console]::Error.WriteLine(
  "The reviewer writes only review.md in the feature folder and scratch files in $script:ReviewerScratchFolder/ of the worktree: $path")
exit 2
