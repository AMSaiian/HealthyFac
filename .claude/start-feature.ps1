#Requires -Version 7
# start-feature.ps1 - opens the three named Claude Code sessions of one feature (spec, executor, reviewer) in the
# feature worktree, each in its own Windows Terminal tab.
#
# The sessions run in the worktree, not in the main checkout, because Claude Code takes the Stop hook script and the
# rules from the project root in which a session starts, and the hook verifies the checkout of its own script file:
# a session that starts in the main checkout never formats, builds, or lints the files of the worktree.
# - Without -Resume: creates the worktree <main checkout>-worktrees\<feature> on the branch feature/<feature> from
#   -MainBranch when the branch has no worktree yet, then starts each session with its role command. The executor and
#   the reviewer wait for a handoff.
# - With -Resume: reopens the sessions by name (claude --resume '<feature>-<role>'), in the worktree of the branch.
#   It creates the worktree too when the branch has none, so a feature whose sessions started in the main checkout
#   moves into its worktree.
# Each tab sets HEALTHYFAC_SESSION_ROLE (the Stop hook does nothing in a reviewer session), opens with the Windows
# Terminal profile -TerminalProfile (name or GUID), runs pwsh with the user profile, and stays open after claude exits.
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
  else { git -C $main worktree add $worktree -b $branch $MainBranch }
  if ($LASTEXITCODE -ne 0) { Write-Error "git worktree add failed for $worktree."; exit 1 }
  Write-Host "Created the worktree $worktree on $branch. Run 'npm ci' in its frontend folder before the first frontend test (docs/local-setup.md)."
}

$sessions = @(
  @{ Role = 'spec'; Model = 'opus'; Command = 'spec' },
  @{ Role = 'executor'; Model = 'sonnet'; Command = 'implement' },
  @{ Role = 'reviewer'; Model = 'opus'; Command = 'review' }
)

# The tab command holds no semicolon: Windows Terminal reads a semicolon as the separator of its own commands and
# would start the text after it as a new tab. Set-Item with && sets the role and then starts claude.
function New-ClaudeCommand($session) {
  $start = if ($Resume) { "--resume '$Feature-$($session.Role)'" } else { "--name '$Feature-$($session.Role)'" }
  $prompt = if ($Resume) { '' } else { " '/$($session.Command) $Feature'" }
  "Set-Item Env:HEALTHYFAC_SESSION_ROLE '$($session.Role)' && claude --chrome $start --model $($session.Model)$prompt"
}

$wtArguments = @('-w', '0')
foreach ($session in $sessions) {
  if ($wtArguments.Count -gt 2) { $wtArguments += ';' }
  $wtArguments += @(
    'new-tab', '-p', $TerminalProfile, '-d', $worktree, '--title', "$Feature $($session.Role)",
    'pwsh', '-NoExit', '-Command', (New-ClaudeCommand $session)
  )
}

wt @wtArguments
