# HealthyFac

HealthyFac is a monitoring and maintenance system for industrial equipment. Read `README.md` for the product domain and goals before you design a feature. Read `docs/architecture.md` for the service boundaries, data ownership, messaging, and identity decisions. Update it when a decision changes.

HealthyFac is built using a strict layered Clean Architecture: Domain, Application, Infrastructure, and Web.

## Repository Layout
*   `backend/src/Core/` — the Core microservice (Domain, Application, Infrastructure, Web).
*   `backend/src/Shared/` — shared layer libraries referenced by every microservice.
*   `backend/tests/` — unit and integration test projects.
*   `docs/` — architecture and design documents.
*   `frontend/` — Angular 22 app (PrimeNG, Tailwind, ngx-translate, SignalStore).
*   `.claude/` — rules, commands, hooks, and the flow scripts. `scripts/` — the verify stack.

## Build and Test
The repo has two test areas. A change in one area needs only the full commands of that area.
*   **Backend area** (every path outside `frontend/` and `keycloak-theme/`): xUnit v3 on Microsoft Testing Platform (set in `global.json`). A full run is the build and two test commands. `--test-modules` runs the built files and does not build, so the build comes first. Run the integration command only when the unit command passes.
    *   Build: `dotnet build backend/HealthyFac.slnx`
    *   Unit and architecture tests (no containers, default limit): `dotnet test --test-modules "backend/tests/**/bin/Debug/net10.0/HealthyFac.*.UnitTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ArchitectureTests.dll"`
    *   Integration and API tests (containers, at most 4 projects at a time): `dotnet test --test-modules "backend/tests/**/bin/Debug/net10.0/HealthyFac.*.IntegrationTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ApiTests.dll" --max-parallel-test-modules 4`
    *   Without `--max-parallel-test-modules`, `dotnet test` runs `Environment.ProcessorCount` test projects at a time. The unit command uses that default.
    *   One project: `dotnet test --project <project> --filter <VSTest filter>`.
*   **Frontend area** (`frontend/`, `keycloak-theme/`): `cd frontend && npm run lint && npx ng build && npx ng test --watch=false`. When `keycloak-theme/` changed, also `cd keycloak-theme && npm run lint && npm test`.
*   **Script tests:** after a change under `.claude/` or `scripts/`, run the test script of the changed file: `pwsh .claude/tests/flow-scripts.tests.ps1`, `pwsh .claude/tests/rules.tests.ps1`, `pwsh .claude/hooks/tests/hook-scope.tests.ps1`, or `pwsh scripts/tests/verify-slots.tests.ps1`. `check-handoff.ps1` runs them for a feature.

## Rules
`.claude/rules/` holds every rule, one file per rule, with its full text. That directory is the single source of truth.

*   `paths` is the only frontmatter field that Claude Code reads. A rule without `paths` loads at launch.
*   A scoped rule loads when Claude reads, writes, or edits a file that its paths match. It can arrive after the first write of a new file. Before you create a file in a path that you did not read yet, read its rule from the table.
*   Never import a rule body with `@`. Claude Code loads an imported file at launch, also from a scoped rule, so the scope is lost.
*   `backend-general` and `frontend-general` have no `paths` on purpose: no tool checks their rules, so they stay loaded at all times. Keep `frontend-general` short: it also loads in backend sessions.
*   No rule describes a layout that a tool applies (the JetBrains cleanup, Prettier, ESLint). A rule holds only what no tool applies.
*   `pwsh .claude/tests/rules.tests.ps1` checks the table, the frontmatter, and the imports.

| Rule | Paths |
| --- | --- |
| `agent-output` | always loaded |
| `agent-workflow` | always loaded |
| `backend-general` | always loaded |
| `frontend-general` | always loaded |
| `git-commits` | always loaded |
| `backend-packages` | `**/*.csproj`, `**/*.props` |
| `backend-domain` | `**/*.Domain/**` |
| `backend-application` | `**/*.Application/**` |
| `backend-infrastructure` | `**/*.Infrastructure/**` |
| `backend-web` | `**/*.Web/**` |
| `testing` | `backend/tests/**`, `**/*Tests/**`, `**/*Test/**` |
| `observability` | `backend/src/**`, `**/Observability/**`, `**/Tracing/**`, `**/Metrics/**`, `**/Logging/**` |
| `frontend-angular` | `frontend/**`, `keycloak-theme/**` |
| `container` | `**/Dockerfile*`, `**/.dockerignore`, `**/docker-compose*.yml`, `**/docker-compose*.yaml`, `docker/**` |

Claude Code matches the paths against the path relative to the repository root.

To add a rule: add `.claude/rules/<name>.md` with its `paths` frontmatter and its text, then add a row to the table.

## Rules Override Skills
Repository rules override plugin skills and their workflows. This includes the TDD, planning, and brainstorming skills. When a skill conflicts with a rule, follow the rule. When you dispatch a subagent, copy the rules that apply to its files into its prompt.

## Verification Hook
The Stop and SubagentStop hooks (`.claude/hooks/verify-changed.ps1`) reformat the changed files, build the solution, and lint the web projects. Fix every reported item before you finish the turn.

*   The hooks check only the checkout (the main one or a worktree) that holds the hook script: the project root in which the session started. Git resolves it; the current folder of the session plays no part. Start each feature session in its feature worktree (`.claude/start-feature.ps1`), or the hooks never see its files.
*   The flow scripts (`tree-id.ps1`, `check-handoff.ps1`, `scripts/verify-stack.ps1`) act on the checkout that holds the script file too. Run the copy of the worktree that you work in.
*   The hooks do nothing when the turn changed no file, and in a reviewer session (`HEALTHYFAC_SESSION_ROLE=reviewer`).
*   Sessions that share one checkout share its changed files. The script headers explain the lock, the skips, and the guard against other writers. `.claude/hooks/tests/hook-scope.tests.ps1` checks them.
