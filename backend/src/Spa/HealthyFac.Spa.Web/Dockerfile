# Build context is the "backend" directory, so the central package files are reachable. The "frontend" directory
# comes in as the named extra context "frontend" (Compose: build.additional_contexts).
#   docker build -f src/Spa/HealthyFac.Spa.Web/Dockerfile --build-context frontend=frontend backend

# ---------- web build ----------
# The stage must not be named "frontend": COPY --from=frontend would then read the stage, not the extra context.
FROM node:24.15.0-alpine3.22 AS web-build
WORKDIR /web

ENV npm_config_update_notifier=false \
    npm_config_fund=false

# The lock file and the manifest come first, so this layer caches across source edits.
COPY --from=frontend package.json package-lock.json ./
RUN npm ci

COPY --from=frontend . ./
RUN npx ng build --configuration production

# ---------- restore ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble AS restore
WORKDIR /src

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# Copy only the files that affect restore, so this layer caches across source edits.
COPY Directory.Build.props Directory.Packages.props BannedSymbols.txt HealthyFac.slnx ./
COPY src/Spa/HealthyFac.Spa.Web/*.csproj                   src/Spa/HealthyFac.Spa.Web/
COPY src/Shared/HealthyFac.Shared.Domain/*.csproj          src/Shared/HealthyFac.Shared.Domain/
COPY src/Shared/HealthyFac.Shared.Application/*.csproj     src/Shared/HealthyFac.Shared.Application/
COPY src/Shared/HealthyFac.Shared.Infrastructure/*.csproj  src/Shared/HealthyFac.Shared.Infrastructure/
COPY src/Shared/HealthyFac.Shared.Web/*.csproj             src/Shared/HealthyFac.Shared.Web/
COPY src/Tools/HealthyFac.HealthProbe/*.csproj             src/Tools/HealthyFac.HealthProbe/

RUN dotnet restore src/Spa/HealthyFac.Spa.Web/HealthyFac.Spa.Web.csproj
RUN dotnet restore src/Tools/HealthyFac.HealthProbe/HealthyFac.HealthProbe.csproj

# ---------- publish ----------
FROM restore AS publish
COPY src/ src/

# UseAppHost=false drops the native launcher, because the entry point calls "dotnet" directly.
RUN dotnet publish src/Spa/HealthyFac.Spa.Web/HealthyFac.Spa.Web.csproj \
    --configuration Release \
    --output /app \
    -p:UseAppHost=false

# The thin health probe. The runtime image has no shell and no curl, so the container health check runs it.
RUN dotnet publish src/Tools/HealthyFac.HealthProbe/HealthyFac.HealthProbe.csproj \
    --configuration Release \
    --output /probe \
    -p:UseAppHost=false

# ---------- runtime ----------
# Chiseled carries the ASP.NET Core runtime only. It has no shell, no package manager, and no SDK.
# The host has no database, so it needs no ICU or tzdata and uses the smaller image without "extra".
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble-chiseled AS final
WORKDIR /app

# Kestrel binds 8080 (the app) and 8090 (the health endpoints), because a non-root user cannot bind a privileged
# port. Keep 8090 equal to HealthEndpoints:Port in appsettings.json.
ENV ASPNETCORE_HTTP_PORTS=8080;8090 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY --from=publish --chown=$APP_UID:$APP_UID /app ./
COPY --from=publish --chown=$APP_UID:$APP_UID /probe ./probe/
# The files of the built app. Spa:ContentRoot defaults to "wwwroot" in the application base directory.
COPY --from=web-build --chown=$APP_UID:$APP_UID /web/dist/healthyfac/browser ./wwwroot/

EXPOSE 8080 8090

# The chiseled images define APP_UID as a non-root account. The app never runs as root.
USER $APP_UID

# Exec form, so "dotnet" becomes PID 1 and receives SIGTERM directly.
ENTRYPOINT ["dotnet", "HealthyFac.Spa.Web.dll"]

# The probe takes the URI to call. Keep the port equal to the health port in ASPNETCORE_HTTP_PORTS above and to
# HealthEndpoints:Port in appsettings.json, because the exec form cannot expand variables.
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
    CMD ["dotnet", "probe/HealthyFac.HealthProbe.dll", "http://127.0.0.1:8090/health/ready"]
