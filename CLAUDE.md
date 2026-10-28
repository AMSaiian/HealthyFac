@AGENTS.md

@README.md

## Claude Code Rule Loading
`.claude/rules/` holds one small file per rule. Each file imports the rule body from `.agents/rules/`. A scoped file declares a `paths` frontmatter list. Claude Code reads only `paths`. It ignores `globs` and loads a file with `globs` in every session. Claude Code loads a scoped rule when it reads a file that its paths match. A file without `paths` loads in every session.

`backend-general` has no `paths` on purpose. A scoped rule can load after Claude writes a new file, and no tool checks the `backend-general` rules, so they stay loaded at all times. `format-csharp` is scoped, because the Stop hook reformats and builds every changed C# file.

| Rule | Paths |
| --- | --- |
| `agent-output` | always loaded |
| `agent-workflow` | always loaded |
| `backend-general` | always loaded |
| `git-commits` | always loaded |
| `format-csharp` | `**/*.cs` |
| `backend-packages` | `**/*.csproj`, `**/*.props` |
| `format-json` | `**/*.json` |
| `backend-domain` | `**/*.Domain/**` |
| `backend-application` | `**/*.Application/**` |
| `backend-infrastructure` | `**/*.Infrastructure/**` |
| `backend-web` | `**/*.Web/**` |
| `testing` | `backend/tests/**`, `**/*Tests/**`, `**/*Test/**` |
| `format-typescript` | `**/*.{ts,tsx,js,jsx}` |
| `format-html` | `**/*.{html,htm}` |
| `format-styles` | `**/*.{css,scss,sass,less}` |
| `container` | `**/Dockerfile*`, `**/.dockerignore`, `**/docker-compose*.{yml,yaml}`, `docker/**` |

Paths use gitignore semantics. Claude Code matches them against the path relative to the repository root.

To add a rule:
1.  Write the content in `.agents/rules/<name>.md` with `globs` frontmatter for Antigravity and Windsurf.
2.  Add `.claude/rules/<name>.md` with the same patterns under `paths` and an `@../../.agents/rules/<name>.md` import.
3.  Add a row to the table above.

## Rules Override Skills
Repository rules override plugin skills and their workflows. This includes the TDD, planning, and brainstorming skills. When a skill conflicts with a rule, follow the rule. When you dispatch a subagent, copy the rules that apply to its files into its prompt.

## Verification Hook
The Stop hook (`.claude/hooks/verify-changed.ps1`) reformats changed C# files and builds the solution. It reports build warnings and errors in those files. Fix every reported item before you finish the turn.

This file imports `AGENTS.md` on purpose. Claude Code skips `AGENTS.md` when a `CLAUDE.md` exists, so the import keeps the shared project context in scope. It also imports `README.md`, so every session knows the product domain. Put shared, agent-agnostic content in `AGENTS.md`. Keep this file for Claude Code specifics only.
