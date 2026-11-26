#Requires -Version 7
# PreToolUse hook of the executor subagent (.claude/agents/executor.md, frontmatter): run-area.ps1 and run-tests.ps1
# run in the foreground. A run in the background ends the executor turn while the run still works, and the executor
# then reports before the full run ends. The hook reads the PreToolUse JSON from stdin. When tool_input.run_in_background
# is true and tool_input.command names run-area.ps1 or run-tests.ps1, it prints the deny decision (exit code 0) with the
# reason. Every other call gets no output.

$ErrorActionPreference = 'Stop'
$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$isBackground = $hookInput.tool_input.run_in_background -eq $true
$command = [string]$hookInput.tool_input.command
if (-not $isBackground -or $command -notmatch 'run-(area|tests)\.ps1') { exit 0 }

@{
  hookSpecificOutput = @{
    hookEventName            = 'PreToolUse'
    permissionDecision       = 'deny'
    permissionDecisionReason = 'Run run-area.ps1 and run-tests.ps1 in the foreground with timeout 600000 (.claude/agents/executor.md, Execution).'
  }
} | ConvertTo-Json -Compress -Depth 5
exit 0
