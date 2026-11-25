# The seed job image. The build context is the "backend" directory, so the central package files are reachable.
#   docker build -f src/Tools/HealthyFac.Seeder/Dockerfile backend

# ---------- restore ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble AS restore
WORKDIR /src

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# Copy only the files that affect restore, so this layer caches across source edits.
COPY Directory.Build.props Directory.Packages.props BannedSymbols*.txt HealthyFac.slnx ./
COPY src/Core/HealthyFac.Core.Domain/*.csproj src/Core/HealthyFac.Core.Domain/
COPY src/Core/HealthyFac.Core.Application/*.csproj src/Core/HealthyFac.Core.Application/
COPY src/Core/HealthyFac.Core.Messaging/*.csproj src/Core/HealthyFac.Core.Messaging/
COPY src/Core/HealthyFac.Core.Infrastructure/*.csproj src/Core/HealthyFac.Core.Infrastructure/
COPY src/Telemetry/HealthyFac.Telemetry.Domain/*.csproj src/Telemetry/HealthyFac.Telemetry.Domain/
COPY src/Telemetry/HealthyFac.Telemetry.Application/*.csproj src/Telemetry/HealthyFac.Telemetry.Application/
COPY src/Telemetry/HealthyFac.Telemetry.Messaging/*.csproj src/Telemetry/HealthyFac.Telemetry.Messaging/
COPY src/Telemetry/HealthyFac.Telemetry.Contracts/*.csproj src/Telemetry/HealthyFac.Telemetry.Contracts/
COPY src/Telemetry/HealthyFac.Telemetry.Infrastructure/*.csproj src/Telemetry/HealthyFac.Telemetry.Infrastructure/
COPY src/Document/HealthyFac.Document.Domain/*.csproj src/Document/HealthyFac.Document.Domain/
COPY src/Document/HealthyFac.Document.Application/*.csproj src/Document/HealthyFac.Document.Application/
COPY src/Document/HealthyFac.Document.Messaging/*.csproj src/Document/HealthyFac.Document.Messaging/
COPY src/Document/HealthyFac.Document.Contracts/*.csproj src/Document/HealthyFac.Document.Contracts/
COPY src/Document/HealthyFac.Document.Infrastructure/*.csproj src/Document/HealthyFac.Document.Infrastructure/
COPY src/Insights/HealthyFac.Insights.Domain/*.csproj src/Insights/HealthyFac.Insights.Domain/
COPY src/Insights/HealthyFac.Insights.Application/*.csproj src/Insights/HealthyFac.Insights.Application/
COPY src/Insights/HealthyFac.Insights.Messaging/*.csproj src/Insights/HealthyFac.Insights.Messaging/
COPY src/Insights/HealthyFac.Insights.Infrastructure/*.csproj src/Insights/HealthyFac.Insights.Infrastructure/
COPY src/Auth/HealthyFac.Auth.Application/*.csproj src/Auth/HealthyFac.Auth.Application/
COPY src/Auth/HealthyFac.Auth.Messaging/*.csproj src/Auth/HealthyFac.Auth.Messaging/
COPY src/Auth/HealthyFac.Auth.Infrastructure/*.csproj src/Auth/HealthyFac.Auth.Infrastructure/
COPY src/Shared/HealthyFac.Shared.Domain/*.csproj src/Shared/HealthyFac.Shared.Domain/
COPY src/Shared/HealthyFac.Shared.Application/*.csproj src/Shared/HealthyFac.Shared.Application/
COPY src/Shared/HealthyFac.Shared.Infrastructure/*.csproj src/Shared/HealthyFac.Shared.Infrastructure/
COPY src/Tools/HealthyFac.MachineSimulator.Scenarios/*.csproj src/Tools/HealthyFac.MachineSimulator.Scenarios/
COPY src/Tools/HealthyFac.Seeder/*.csproj src/Tools/HealthyFac.Seeder/

RUN dotnet restore src/Tools/HealthyFac.Seeder/HealthyFac.Seeder.csproj

# ---------- publish ----------
FROM restore AS publish
COPY src/ src/
# The project copies the seed files next to the program (Content links). The Compose job mounts the same folder
# read-only over them, so a change of a seed file needs no new image.
COPY seed/demo/ seed/demo/

# The restore layer already warmed the package cache, so this resolves offline and stays fast.
# UseAppHost=false drops the native launcher, because the entry point calls "dotnet" directly.
RUN dotnet publish src/Tools/HealthyFac.Seeder/HealthyFac.Seeder.csproj \
    --configuration Release \
    --output /app \
    -p:UseAppHost=false

# ---------- runtime ----------
# Chiseled carries the ASP.NET Core runtime only (the service libraries reference it). It has no shell, no package
# manager, and no SDK. The "extra" variant adds ICU and tzdata, which Npgsql needs and the seed needs for the time
# zone of the company.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble-chiseled-extra AS final
WORKDIR /app

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY --from=publish --chown=$APP_UID:$APP_UID /app ./

# The chiseled images define APP_UID as a non-root account. The job never runs as root.
USER $APP_UID

# Exec form, so "dotnet" becomes PID 1 and receives SIGTERM directly. A one-shot job has no health check.
ENTRYPOINT ["dotnet", "HealthyFac.Seeder.dll"]
