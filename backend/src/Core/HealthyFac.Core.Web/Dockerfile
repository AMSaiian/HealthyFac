# Build context is the "backend" directory, so the central package files are reachable.
#   docker build -f src/Core/HealthyFac.Core.Web/Dockerfile backend

# ---------- restore ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble AS restore
WORKDIR /src

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# Copy only the files that affect restore, so this layer caches across source edits.
COPY Directory.Build.props Directory.Packages.props HealthyFac.slnx ./
COPY src/Core/HealthyFac.Core.Domain/*.csproj          src/Core/HealthyFac.Core.Domain/
COPY src/Core/HealthyFac.Core.Application/*.csproj     src/Core/HealthyFac.Core.Application/
COPY src/Core/HealthyFac.Core.Infrastructure/*.csproj  src/Core/HealthyFac.Core.Infrastructure/
COPY src/Core/HealthyFac.Core.Web/*.csproj             src/Core/HealthyFac.Core.Web/
COPY src/Shared/HealthyFac.Shared.Domain/*.csproj          src/Shared/HealthyFac.Shared.Domain/
COPY src/Shared/HealthyFac.Shared.Application/*.csproj     src/Shared/HealthyFac.Shared.Application/
COPY src/Shared/HealthyFac.Shared.Infrastructure/*.csproj  src/Shared/HealthyFac.Shared.Infrastructure/
COPY src/Shared/HealthyFac.Shared.Web/*.csproj             src/Shared/HealthyFac.Shared.Web/
COPY src/Tools/HealthyFac.HealthProbe/*.csproj             src/Tools/HealthyFac.HealthProbe/

RUN dotnet restore src/Core/HealthyFac.Core.Web/HealthyFac.Core.Web.csproj
RUN dotnet restore src/Tools/HealthyFac.HealthProbe/HealthyFac.HealthProbe.csproj

# ---------- publish ----------
FROM restore AS publish
COPY src/ src/

# The restore layer already warmed the package cache, so this resolves offline and stays fast.
# UseAppHost=false drops the native launcher, because the entry point calls "dotnet" directly.
RUN dotnet publish src/Core/HealthyFac.Core.Web/HealthyFac.Core.Web.csproj \
    --configuration Release \
    --output /app \
    -p:UseAppHost=false

# The thin health probe. The runtime image has no shell and no curl, so the container health check runs it.
RUN dotnet publish src/Tools/HealthyFac.HealthProbe/HealthyFac.HealthProbe.csproj \
    --configuration Release \
    --output /probe \
    -p:UseAppHost=false

# The file log sink writes here. A named volume mounted on this path inherits the directory and its owner.
RUN mkdir /app/logs

# ---------- runtime ----------
# Chiseled carries the ASP.NET Core runtime only. It has no shell, no package manager, and no SDK.
# The "extra" variant adds ICU and tzdata, which Npgsql needs for collations and timestamps.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble-chiseled-extra AS final
WORKDIR /app

# Kestrel binds 8080, because a non-root user cannot bind a privileged port.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY --from=publish --chown=$APP_UID:$APP_UID /app ./
COPY --from=publish --chown=$APP_UID:$APP_UID /probe ./probe/

EXPOSE 8080

# The chiseled images define APP_UID as a non-root account. The app never runs as root.
USER $APP_UID

# Exec form, so "dotnet" becomes PID 1 and receives SIGTERM directly.
# A shell form would start /bin/sh -c, which does not forward signals. Docker would then
# kill the container with SIGKILL after the stop timeout, losing graceful shutdown.
ENTRYPOINT ["dotnet", "HealthyFac.Core.Web.dll"]

# The probe takes the URI to call. Keep the port equal to ASPNETCORE_HTTP_PORTS above,
# because the exec form cannot expand variables. The probe exits 0 on a success status. Kubernetes ignores
# this instruction and uses its own probes.
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
    CMD ["dotnet", "probe/HealthyFac.HealthProbe.dll", "http://127.0.0.1:8080/health/ready"]
