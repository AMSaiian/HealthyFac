---
trigger: glob
globs: "**/*.cs"
description: "C# Formatting rules extracted from .editorconfig"
---
# C# Code Formatting Rules (.editorconfig)
`.editorconfig` defines the layout and the syntax style. The JetBrains formatter applies both: run `pwsh .claude/hooks/cleanup-changed.ps1` after you change C# files (the Claude Code Stop and SubagentStop hooks run it for you). It sets 4-space indents, 120 columns, attributes on their own line, max 1 blank line, primary constructor parameters chopped above 4, and long lists, chains, and binary expressions chopped (operator first, no alignment). It also adds braces, expression bodies, `var`, type keywords (`string`, not `String`), names for literal arguments (`AddDays(value: 1)`), and trailing commas in multiline lists, removes `this.`, and it sorts the `using` directives (`System` first) and removes the unused ones (profile `HealthyFac`, `backend/HealthyFac.sln.DotSettings`). It never splits a string literal or a one-line XML doc comment, so the hook reports each line over 120 characters outside `Migrations/`: split it by hand. The build reports `var`, braces, access modifiers, and method groups as warnings. Write code that already matches, and follow the rules that no tool applies:
*   **Private Field Names:** Name private instance fields `_camelCase` (e.g., `_dbContext`, `_isStarted`).
*   **Method Order:** Order methods public, internal, protected, private. Within a group, put a caller above its helpers. Fields, constructors, and properties stay above all methods.
