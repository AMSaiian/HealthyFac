#Requires -Version 7
# PreToolUse hook of every session and subagent (.claude/settings.json, matcher Bash|PowerShell): the deterministic
# check of agent-workflow.md, Handoff Files. The three handoff files of a feature folder (spec.md, plan.md, review.md)
# change only through the Edit tool, because a script once truncated review.md (2026-11-18).
# It denies (exit code 2, the reason on stderr) a shell command that names one of these files and holds a known write
# form: a > or >> redirect or tee to one of the files, sed -i, perl -i, Set-Content, Add-Content, Out-File,
# writeFileSync, appendFileSync, write_text, .write(, WriteAllText, WriteAllLines, or mv, cp, rm, Move-Item, Copy-Item,
# or Remove-Item with one of the files. Reads (cat, grep, head, Get-Content, Select-String), a read redirected to
# another file, and the flow scripts stay allowed. The repo file .claude/commands/spec.md is not a handoff file.
# The forms that name no file (sed -i through WriteAllLines) come from shell-write-forms.ps1, which the reviewer shell
# hook uses too.
# Limit: a pattern check. It can deny a valid command that also names a handoff file (then use the Edit tool or split
# the command), and it misses a write form that is not in the list.

$ErrorActionPreference = 'Stop'
$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$command = "$($hookInput.tool_input.command)"
if (-not $command) { exit 0 }

$File = '(?:spec|plan|review)\.md\b'
$Path = "(?:[^\s'`"|;&<>]*[\\/])?$File"
$text = $command -replace '(?i)\.claude[\\/]+commands[\\/]+spec\.md', ''
if ($text -notmatch "(?i)(^|[\\/\s'`"=(])$File") { exit 0 }

. (Join-Path $PSScriptRoot 'shell-write-forms.ps1')
$WriteForms = @(
  "(?i)>{1,2}\s*['`"]?$Path",
  "(?i)\btee\b[^|;&]*$File",
  "(?i)(^|[\s;&|(])(mv|cp|rm|Move-Item|Copy-Item|Remove-Item)\b[^|;&]*$File"
) + $script:SharedWriteForms
$form = $WriteForms | Where-Object { $text -match $_ } | Select-Object -First 1
if (-not $form) { exit 0 }

[Console]::Error.WriteLine(
  'Change spec.md, plan.md, and review.md only with the Edit tool, one exact replacement per call ' +
  '(agent-workflow.md, Handoff Files). This shell command names a handoff file and writes with a script or a shell ' +
  'command. Use the Edit tool, or split a read from other work.')
exit 2
