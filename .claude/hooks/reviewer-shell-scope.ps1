#Requires -Version 7
# PreToolUse hook of the reviewer subagent (.claude/agents/reviewer.md, frontmatter, matcher Bash|PowerShell): the
# reviewer writes only review.md with the Edit tool (reviewer-write-scope.ps1), never a file through the shell.
# It denies (exit code 2, the reason on stderr) a shell command that holds a known write form: a > or >> redirect to a
# path, tee, New-Item, Copy-Item, Move-Item, Remove-Item, cp, mv, rm, mkdir, or touch as a command word (at the start,
# or after ;, &, |, (, or a space), and the forms of shell-write-forms.ps1 (sed -i, perl -i, Set-Content, Add-Content,
# Out-File, writeFileSync, appendFileSync, write_text, .write(, WriteAllText, WriteAllLines; the handoff guard uses the
# same list). The > redirect check reads the command with each single- or double-quoted string replaced by a word, so
# a > inside an awk program or a git format is no redirect. The quoted string that follows -c, -Command, -e, or
# --eval is a nested program: the redirect check also reads it, with its own inner quoted strings replaced.
# Every other form reads the whole command.
# Allowed forms are removed before the match: a redirect to /dev/null, $null, or NUL (>, >>, 2>, *>, with or without a
# space), a stream merge (2>&1, >&2, 1>&2), and Out-Null. A comparison operator in a PowerShell expression (-gt, -ge)
# holds no > and is no redirect.
# Limit: a pattern check. It can deny a valid command (for example, git rm), and it misses a write form that is not in
# the list. A redirect inside a quoted string that no -c, -Command, -e, or --eval introduces, or inside a nested
# program of a nested program, passes.

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'shell-write-forms.ps1')
$hookInput = try { [Console]::In.ReadToEnd() | ConvertFrom-Json } catch { $null }
$command = "$($hookInput.tool_input.command)"
if (-not $command) { exit 0 }

$AllowedForms = @(
  '(?i)(?:\d|\*|&)?>{1,2}\s*(?:/dev/null|\$null|NUL)\b',
  '\d*>&\d+',
  '(?i)\bOut-Null\b'
)
$text = $command
foreach ($allowed in $AllowedForms) { $text = $text -replace $allowed, '' }

$RedirectForm = '(?<![=<>-])>{1,2}&?\s*[''"]?[^\s>&=''"]'
$QuotedString = '"[^"]*"|''[^'']*'''
$NestedProgram = '(?i)(?:^|\s)(?:-c|-Command|-e|--eval)\s+(?:"(?<program>[^"]*)"|''(?<program>[^'']*)'')'
$redirectTexts = @($text) + @([regex]::Matches($text, $NestedProgram) | ForEach-Object { $_.Groups['program'].Value })
$hasRedirect = @($redirectTexts | Where-Object { [regex]::Replace($_, $QuotedString, 'Q') -match $RedirectForm }).Count -gt 0
$WriteForms = @(
  '(?i)\btee\b',
  '(?i)\b(New-Item|Copy-Item|Move-Item|Remove-Item)\b',
  '(?i)(^|[;&|(\s])(cp|mv|rm|mkdir|touch)\s'
) + $script:SharedWriteForms
$isDenied = $hasRedirect -or @($WriteForms | Where-Object { $text -match $_ }).Count -gt 0
if (-not $isDenied) { exit 0 }

[Console]::Error.WriteLine(
  'The reviewer writes only review.md, with the Edit tool. This shell command writes a file. ' +
  'Read the command output directly instead of a scratch file.')
exit 2
