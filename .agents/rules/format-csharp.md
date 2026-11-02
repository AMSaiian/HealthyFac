---
trigger: glob
globs: "**/*.cs"
description: "C# Formatting rules extracted from .editorconfig"
---
# C# Code Formatting Rules (.editorconfig)
`.editorconfig` defines the layout and the syntax style. The JetBrains formatter applies both: run `pwsh .claude/hooks/cleanup-changed.ps1` after you change C# files (the Claude Code Stop and SubagentStop hooks run it for you). It sets 4-space indents, 120 columns, attributes on their own line, max 1 blank line, primary constructor parameters chopped above 4, and long lists, chains, and binary expressions chopped (operator first, no alignment). It also adds braces, expression bodies, `var`, and trailing commas in multiline lists. The build reports `var`, braces, access modifiers, and method groups as warnings. Write code that already matches, and follow the rules that no tool applies:
*   **Type Keywords:** Use `string`, `int`, `bool`, not `String`, `Int32`, `Boolean`.
*   **No `this.`:** Do not qualify members with `this.`.
*   **Private Field Names:** Name private instance fields `_camelCase` (e.g., `_dbContext`, `_isStarted`).
*   **Method Order:** Order methods public, internal, protected, private. Within a group, put a caller above its helpers. Fields, constructors, and properties stay above all methods.
