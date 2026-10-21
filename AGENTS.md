# HealthyFac

HealthyFac is a monitoring and maintenance system for industrial equipment. Read `README.md` for the product domain and goals before you design a feature.

HealthyFac is built using a strict layered Clean Architecture: Domain, Application, Infrastructure, and Web.

## Repository Layout
*   `backend/src/Core/` — the Core microservice (Domain, Application, Infrastructure, Web).
*   `backend/src/Shared/` — shared layer libraries referenced by every microservice.
*   `backend/tests/` — unit and integration test projects.
*   `frontend/` — frontend application (not yet implemented).

## Build and Test
*   Build: `dotnet build backend/HealthyFac.slnx`
*   Test: `dotnet test backend/HealthyFac.slnx`

## Rules
`.agents/rules/` holds every rule. That directory is the single source of truth. Edit rules there only.

Each rule file declares its scope in `globs` frontmatter. A rule with `trigger: always_on` and no `globs` applies to all work.

*   Read `.agents/rules/agent-output.md` first. It governs every response and all documentation.
*   Read the layer rule for any path you touch (`backend-domain`, `backend-application`, `backend-infrastructure`, `backend-web`).
*   Read `backend-general.md` and `format-csharp.md` for any C# file.
*   Read `backend-packages.md` for `.csproj` and `.props` files.
*   Read `testing.md` for test projects.
*   Read `container.md` for Dockerfiles and Compose files.

Antigravity and Windsurf match these globs directly. Claude Code loads them through `.claude/rules/`; see `CLAUDE.md`.
