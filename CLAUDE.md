@AGENTS.md

## Claude Code Rule Loading
`.claude/rules/` holds one small file per rule. Each file declares a `globs` frontmatter list and imports the rule body from `.agents/rules/`. Claude Code loads a rule when you read, write, or edit a file that its globs match. A file without `globs` loads in every session.

| Rule | Globs |
| --- | --- |
| `agent-output` | always loaded |
| `format-json` | `**/*.json` |
| `backend-general` | `**/*.cs` |
| `format-csharp` | `**/*.cs` |
| `backend-domain` | `**/*.Domain/**` |
| `backend-application` | `**/*.Application/**` |
| `backend-infrastructure` | `**/*.Infrastructure/**` |
| `backend-web` | `**/*.Web/**` |
| `testing` | `backend/tests/**`, `**/*Tests/**`, `**/*Test/**` |
| `format-typescript` | `**/*.{ts,tsx,js,jsx}` |
| `format-html` | `**/*.{html,htm}` |
| `format-styles` | `**/*.{css,scss,sass,less}` |
| `container` | `**/Dockerfile*`, `**/.dockerignore`, `**/docker-compose*.{yml,yaml}`, `docker/**` |

Globs use gitignore semantics. Claude Code matches them against the path relative to the repository root.

To add a rule:
1.  Write the content in `.agents/rules/<name>.md`.
2.  Add `.claude/rules/<name>.md` with its `globs` and an `@../../.agents/rules/<name>.md` import.
3.  Add a row to the table above.

This file imports `AGENTS.md` on purpose. Claude Code skips `AGENTS.md` when a `CLAUDE.md` exists, so the import keeps the shared project context in scope. Put shared, agent-agnostic content in `AGENTS.md`. Keep this file for Claude Code specifics only.
