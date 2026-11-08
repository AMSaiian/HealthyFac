# HealthyFac

HealthyFac is a monitoring and maintenance system for industrial equipment. Read `README.md` for the product domain and goals before you design a feature. Read `docs/architecture.md` for the service boundaries, data ownership, messaging, and identity decisions. Update it when a decision changes.

HealthyFac is built using a strict layered Clean Architecture: Domain, Application, Infrastructure, and Web.

## Repository Layout
*   `backend/src/Core/` — the Core microservice (Domain, Application, Infrastructure, Web).
*   `backend/src/Shared/` — shared layer libraries referenced by every microservice.
*   `backend/tests/` — unit and integration test projects.
*   `docs/` — architecture and design documents.
*   `frontend/` — Angular 22 app (PrimeNG, Tailwind, ngx-translate, SignalStore).

## Build and Test
*   Build: `dotnet build backend/HealthyFac.slnx`
*   Test: xUnit v3 on Microsoft Testing Platform (set in `global.json`). A full run is the build and two test commands. `--test-modules` runs the built files and does not build, so the build comes first. Run the unit command first, and run the integration command only when it passes.
    *   Unit and architecture tests (no containers, default limit): `dotnet test --test-modules "backend/tests/**/bin/Debug/net10.0/HealthyFac.*.UnitTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ArchitectureTests.dll"`
    *   Integration and API tests (containers, at most 4 projects at a time): `dotnet test --test-modules "backend/tests/**/bin/Debug/net10.0/HealthyFac.*.IntegrationTests.dll;backend/tests/**/bin/Debug/net10.0/HealthyFac.*.ApiTests.dll" --max-parallel-test-modules 4`
    *   Without `--max-parallel-test-modules`, `dotnet test` runs `Environment.ProcessorCount` test projects at a time. The unit command uses that default.
*   One project: `dotnet test --project <project> --filter <VSTest filter>`.
*   Frontend: `cd frontend && npm run lint && npx ng build && npx ng test --watch=false`

## Rules
`.agents/rules/` holds every rule. That directory is the single source of truth. Edit rules there only.

Each rule file declares its scope in `globs` frontmatter. A rule with `trigger: always_on` and no `globs` applies to all work.

*   Read `.agents/rules/agent-output.md` first. It governs every response and all documentation.
*   Read `.agents/rules/agent-workflow.md` before you change anything. It governs scope, removals, and the final report.
*   Read the layer rule for any path you touch (`backend-domain`, `backend-application`, `backend-infrastructure`, `backend-web`).
*   Read `backend-general.md` and `format-csharp.md` for any C# file.
*   Read `backend-packages.md` for `.csproj` and `.props` files.
*   Read `testing.md` for test projects.
*   Read `frontend-angular.md` for the frontend.
*   Read `container.md` for Dockerfiles and Compose files.
*   Read `observability.md` for telemetry code (resource attributes, spans, metrics, log export).
*   Read `git-commits.md` before you write a commit.

Antigravity and Windsurf match these globs directly. Claude Code loads them through `.claude/rules/`; see `CLAUDE.md`.
