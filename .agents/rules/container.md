---
trigger: glob
globs: ["**/Dockerfile*", "**/.dockerignore", "**/docker-compose*.yml", "**/docker-compose*.yaml", "docker/**"]
description: "Rules for Dockerfiles and Compose files."
---
# Container Rules
*   **Multi-Stage Builds:** Build in a separate stage from the final stage. The final stage copies only the published output from the build stage.
*   **Runtime-Only Final Image:** The final image holds the runtime and the files the app needs. It must not hold an SDK, compiler, build tool, package manager, shell, or source code. Use a chiseled or distroless base image for .NET services.
*   **Image Verification:** Prove the final image is clean. Inspect its layers or run `docker history` and `docker run --entrypoint` checks. Do not assume. Repeat the check after you change the base image or the Dockerfile.
*   **No Root User:** Never run our own services as root. Set a numeric `USER` in the Dockerfile (for .NET, `USER $APP_UID`). For third-party images, set a non-root `user:` in Compose only when it works without errors. Test the change. If the image breaks, keep its default user and add a Compose comment with the reason.
*   **Least Privilege:** Set `cap_drop: [ALL]`, `security_opt: [no-new-privileges:true]`, `read_only: true`, and `tmpfs` for writable paths on every service that we build. Apply the same settings to a third-party image only when it works without errors. Add a capability back only with a written reason.
*   **Graceful Stop:** The app process must receive the stop signal. Write `ENTRYPOINT` and `CMD` in exec form (a JSON array), never in shell form. A shell wrapper hides the signal from the app. Run no wrapper script unless it ends with `exec`.
*   **Stop Settings:** Set `stop_signal` and `stop_grace_period` for every service. Use `SIGTERM` for .NET services. Use the signal that the database documents for stateful services (for example, `SIGINT` for PostgreSQL fast shutdown). Never rely on a forced kill after the grace period.
*   **Stop Verification:** Run `docker compose stop` for a changed service. Confirm that the exit code is 0 and that the stop time is below the grace period.
*   **Pinned Images:** Pin every image to an exact version tag. Never use `latest` or a floating major tag.
*   **No Secrets in Images:** Never copy `.env` files or secrets into an image. List `.env*` (except `.env.example`) in `.dockerignore`. Pass secrets as runtime environment variables.
*   **Build Context:** Use `backend` as the build context for every .NET service, so each can reach `Shared`. Keep one strict `backend/.dockerignore` for all of them. Add a `<Dockerfile>.dockerignore` only when one service needs different rules. That file replaces the shared one, so repeat every shared pattern in it. Exclude secrets, tests, build output, logs, and agent folders.
*   **Naming:** Prefix every container name with `healthyfac-`. Set the Compose project `name` to `healthyfac`.
*   **Ports from Environment:** Read every host port from `.env` with a default (for example, `${CORE_PORT:-8080}`). Keep container ports fixed. Add each new variable to `.env.example`. Read the public URLs from `HEALTHYFAC_AUTH_URL` and `HEALTHYFAC_SPA_URL`, and the route host names from `HEALTHYFAC_DOMAIN` (`backend-web.md`, rule **Public URLs**). The gateway publishes HTTPS only on `GATEWAY_PORT` (default 443).
*   **Startup Order:** Add a `healthcheck` to every stateful service and to the observability stack. Use `depends_on` with `condition: service_healthy` for each hard dependency. An app starts only after its database and its log and trace receiver accept data. App images have no shell or `curl`, so each app Dockerfile publishes the shared `HealthyFac.HealthProbe` console app, copies it to `probe/`, and defines `HEALTHCHECK CMD ["dotnet", "probe/HealthyFac.HealthProbe.dll", "<readiness-uri>"]`. The readiness URI uses the health port (`backend-web.md`, rule **Health Endpoints**). Do not write a different probe per service. In production, point the platform HTTP probes at `/health` and `/health/ready` on the health port.
*   **Persistent Data:** Store data in named volumes. Never store state in the container layer.
