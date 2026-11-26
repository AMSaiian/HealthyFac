#Requires -Version 7
# start-feature.ps1 - opens the Claude Code session of one feature (the spec writer) in the feature worktree, in a
# Windows Terminal tab. The spec writer runs the executor and the reviewer as subagents (.claude/agents/), so the
# feature needs no other session.
#
# The session runs in the worktree, not in the main checkout, because Claude Code takes the Stop hook script and the
# rules from the project root in which a session starts, and the hook verifies the checkout of its own script file:
# a session that starts in the main checkout never formats, builds, or lints the files of the worktree. The subagents
# work in the checkout of the session.
# - Without -Resume: creates the worktree <main checkout>-worktrees\<feature> on the branch feature/<feature> when the
#   branch has no worktree yet: it fetches origin/<-MainBranch> and starts the branch from it (else from the local
#   -MainBranch), with --no-track, then starts the session '<feature>-spec' with '/spec <feature>'.
# - With -Resume: reopens the session by name (claude --resume '<feature>-spec'), in the worktree of the branch. The
#   resumed session can resume its executor by its agent ID (agents.md in the feature folder); each reviewer task
#   starts a new reviewer. It creates the worktree too when the branch has none.
# The tab opens with the Windows Terminal profile -TerminalProfile (name or GUID), runs pwsh with the user profile, and
# stays open after claude exits. --chrome gives the session and its subagents Claude in Chrome for the browser checks.
# --effort high sets the effort of the spec session (new and resumed); the executor and the reviewer set their own in
# their frontmatter (.claude/agents/).
# Usage, from any folder (the script finds the repo from its own location):
#   pwsh .claude/start-feature.ps1 late-fees
#   pwsh .claude/start-feature.ps1 late-fees -Resume
param(
  [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Feature,
  [switch]$Resume,
  [string]$MainBranch = 'develop',
  [string]$TerminalProfile = 'PowerShell'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'hooks/checkout-helpers.ps1')
$commonDir = git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir 2>$null
if ($LASTEXITCODE -ne 0 -or -not $commonDir) { Write-Error 'This script is not inside a git checkout.'; exit 1 }
$main = (Split-Path $commonDir -Parent) -replace '/', '\'
$branch = "feature/$Feature"

# The worktree that has the branch checked out, or nothing.
function Get-BranchWorktree {
  $path = $null
  foreach ($line in @(git -C $main worktree list --porcelain)) {
    if ($line -like 'worktree *') { $path = $line.Substring(9) }
    elseif ($line -eq "branch refs/heads/$branch") { return ($path -replace '/', '\') }
  }
}

$worktree = Get-BranchWorktree
if (-not $worktree) {
  $worktree = Join-Path (Split-Path $main -Parent) "$(Split-Path $main -Leaf)-worktrees\$Feature"
  git -C $main show-ref --verify --quiet "refs/heads/$branch"
  if ($LASTEXITCODE -eq 0) { git -C $main worktree add $worktree $branch }
  else {
    git -C $main fetch origin $MainBranch 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "Note: git fetch origin $MainBranch failed; the new branch starts from the local ref." }
    $startPoint = Get-MainBranchRef $main $MainBranch 'the new branch starts from'
    git -C $main worktree add --no-track $worktree -b $branch $startPoint
  }
  if ($LASTEXITCODE -ne 0) { Write-Error "git worktree add failed for $worktree."; exit 1 }
  Write-Host "Created the worktree $worktree on $branch. Run 'npm ci' in its frontend folder before the first frontend test (docs/local-setup.md)."
}

$session = "$Feature-spec"
$start = if ($Resume) { "--resume '$session'" } else { "--name '$session' --model opus '/spec $Feature'" }

# The tab command holds no semicolon: Windows Terminal reads a semicolon as the separator of its own commands and
# would start the text after it as a new tab.
wt -w 0 new-tab -p $TerminalProfile -d $worktree --title "$Feature spec" pwsh -NoExit -Command "claude --chrome --effort high $start"
