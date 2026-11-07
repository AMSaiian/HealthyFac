# verify-stack.ps1 - starts, shows, and removes the isolated verify stack, so an agent (or you) can check the UI in
# a browser next to the running dev stack. It never touches the dev project "healthyfac".
#
# Usage, from anywhere inside the repo:
#   pwsh scripts/verify-stack.ps1 up       builds and starts the stack, waits until it is ready, prints the sign-in
#   pwsh scripts/verify-stack.ps1 status   lists the containers of the stack
#   pwsh scripts/verify-stack.ps1 down     removes the containers, the volumes, and the images of the stack
#
# Configuration: Compose reads .env (the secrets, shared with the dev stack) and then .env.verify (the overrides: the
# domain, the public URLs, and the published ports). docker/verify/docker-compose.verify.yml renames the containers
# and images and removes every other host port.
#
# One-time setup:
# - Add this line to C:\Windows\System32\drivers\etc\hosts as an administrator:
#     127.0.0.1 api.hfverify.test auth.hfverify.test app.hfverify.test
# - mkcert must be installed (docker/certs/README.md). The script creates the certificate when it is missing.
# - .env must exist (copy .env.example).
param(
  [Parameter(Mandatory = $true, Position = 0)]
  [ValidateSet('up', 'status', 'down')]
  [string]$Command
)

$ErrorActionPreference = 'Stop'

$Project = 'healthyfac-verify'
$DevProject = 'healthyfac'
$ReadyTimeout = [TimeSpan]::FromMinutes(15)
$AppTimeout = [TimeSpan]::FromMinutes(2)
$PollInterval = [TimeSpan]::FromSeconds(5)
$ReadyServices = @('core', 'telemetry', 'auth', 'spa', 'gateway')
$RealmJob = 'keycloak-config'
$BuiltImages = @('core', 'telemetry', 'auth', 'spa', 'gateway') | ForEach-Object { "healthyfac-verify/${_}:local" }
$HostsFile = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'

$repo = git rev-parse --show-toplevel 2>$null
if (-not $repo) { Write-Error 'Run this script from inside the repo.' }
$repo = $repo -replace '/', '\'
$envFile = Join-Path $repo '.env'
$verifyEnvFile = Join-Path $repo '.env.verify'
$certFile = Join-Path $repo 'docker\certs\hfverify.test.pem'
$keyFile = Join-Path $repo 'docker\certs\hfverify.test-key.pem'

if ($Project -eq $DevProject) { Write-Error "Refusing to run: the project name equals the dev project '$DevProject'." }
foreach ($file in $envFile, $verifyEnvFile) {
  if (-not (Test-Path $file)) { Write-Error "Missing $file. See the header of this script." }
}

$composeArgs = @(
  'compose', '-p', $Project, '--project-directory', $repo,
  '--env-file', $envFile, '--env-file', $verifyEnvFile,
  '-f', (Join-Path $repo 'docker-compose.yml'),
  '-f', (Join-Path $repo 'docker\verify\docker-compose.verify.yml')
)

function Invoke-Compose {
  & docker @composeArgs @args
  if ($LASTEXITCODE -ne 0) { Write-Error "docker compose $($args -join ' ') failed with exit code $LASTEXITCODE." }
}

# Reads the values after Compose resolved both env files, so the script never parses them itself.
function Get-ResolvedConfig {
  $json = & docker @composeArgs config --format json
  if ($LASTEXITCODE -ne 0) { Write-Error 'docker compose config failed. Check .env and .env.verify.' }
  $config = $json | ConvertFrom-Json
  $spaUrl = [Uri]$config.services.core.environment.InviteLinks__SpaBaseUrl
  return [pscustomobject]@{
    SpaUrl          = $spaUrl.AbsoluteUri.TrimEnd('/')
    Domain          = $spaUrl.Host -replace '^app\.', ''
    GatewayPort     = [int]($config.services.gateway.ports | Select-Object -First 1).published
    MailpitPort     = [int]($config.services.mailpit.ports | Select-Object -First 1).published
    SupervisorEmail = $config.services.core.environment.Seeding__DevelopmentData__TestTenant__SupervisorEmail
    SupervisorPassword = $config.services.'keycloak-config'.environment.HEALTHYFAC_TEST_SUPERVISOR_PASSWORD
  }
}

function Assert-HostNames([string[]]$hostNames) {
  $hosts = Get-Content $HostsFile -ErrorAction SilentlyContinue
  $missing = $hostNames | Where-Object {
    $name = $_
    -not ($hosts | Where-Object { $_ -match "^\s*127\.0\.0\.1\s.*\b$([regex]::Escape($name))\b" })
  }
  if ($missing) {
    Write-Error ("The hosts file lacks: $($missing -join ', '). Add this line to $HostsFile as an administrator:`n" +
      "  127.0.0.1 $($hostNames -join ' ')")
  }
}

function Assert-Certificate([string[]]$hostNames) {
  if ((Test-Path $certFile) -and (Test-Path $keyFile)) { return }
  if (-not (Get-Command mkcert -ErrorAction SilentlyContinue)) {
    Write-Error 'The verify certificate is missing and mkcert is not installed. See docker/certs/README.md.'
  }
  & mkcert -cert-file $certFile -key-file $keyFile @hostNames
  if ($LASTEXITCODE -ne 0) { Write-Error 'mkcert failed to create the verify certificate.' }
}

function Assert-PortFree([int]$port, [string]$name) {
  $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
  if (-not $listener) { return }
  $ownContainer = docker ps --filter "label=com.docker.compose.project=$Project" --format '{{.Ports}}' |
    Where-Object { $_ -match ":$port->" }
  if (-not $ownContainer) { Write-Error "Port $port ($name) is in use. Set $name in .env.verify to a free port." }
}

function Get-ContainerState([string]$service) {
  $state = docker inspect --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{end}}|{{.State.ExitCode}}' "$Project-$service" 2>$null
  if ($LASTEXITCODE -ne 0 -or -not $state) { return $null }
  $parts = $state -split '\|'
  return [pscustomobject]@{ Status = $parts[0]; Health = $parts[1]; ExitCode = [int]$parts[2] }
}

function Show-FailureAndExit([string]$service, [string]$reason) {
  Write-Host "The verify stack is not ready: $service $reason." -ForegroundColor Red
  & docker @composeArgs logs --tail 60 $service
  exit 1
}

function Wait-Ready {
  $deadline = [DateTime]::UtcNow + $ReadyTimeout
  while ([DateTime]::UtcNow -lt $deadline) {
    $job = Get-ContainerState $RealmJob
    if ($job -and $job.Status -eq 'exited' -and $job.ExitCode -ne 0) { Show-FailureAndExit $RealmJob "exited with code $($job.ExitCode)" }

    $pending = @()
    foreach ($service in $ReadyServices) {
      $state = Get-ContainerState $service
      if ($state -and ($state.Status -eq 'exited' -or $state.Health -eq 'unhealthy')) {
        Show-FailureAndExit $service "is $($state.Status) $($state.Health)".Trim()
      }
      if (-not $state -or $state.Health -ne 'healthy') { $pending += $service }
    }
    if ($pending.Count -eq 0) { return }
    Write-Host "Waiting for: $($pending -join ', ')"
    Start-Sleep -Seconds $PollInterval.TotalSeconds
  }
  Write-Host "The verify stack did not get ready within $($ReadyTimeout.TotalMinutes) minutes." -ForegroundColor Red
  Invoke-Compose ps
  exit 1
}

function Wait-AppAnswers([string]$url) {
  $deadline = [DateTime]::UtcNow + $AppTimeout
  while ([DateTime]::UtcNow -lt $deadline) {
    try {
      $response = Invoke-WebRequest -Uri $url -Method Get -SkipHttpErrorCheck -TimeoutSec 10
      if ($response.StatusCode -eq 200) { return }
    } catch {
      # The gateway answers only after its routes load; retry until the deadline.
    }
    Start-Sleep -Seconds $PollInterval.TotalSeconds
  }
  Write-Error "The app does not answer 200 at $url."
}

switch ($Command) {
  'up' {
    $config = Get-ResolvedConfig
    $hostNames = @('api', 'auth', 'app') | ForEach-Object { "$_.$($config.Domain)" }
    Assert-HostNames $hostNames
    Assert-Certificate $hostNames
    Assert-PortFree $config.GatewayPort 'GATEWAY_PORT'
    Assert-PortFree $config.MailpitPort 'MAILPIT_UI_PORT'
    Invoke-Compose up -d --build
    Wait-Ready
    Wait-AppAnswers $config.SpaUrl
    # The test supervisor is a development-only account of the seeded test tenant; the agent needs it to sign in.
    Write-Host ''
    Write-Host 'The verify stack is ready.' -ForegroundColor Green
    Write-Host "App:     $($config.SpaUrl)"
    Write-Host "Sign in: $($config.SupervisorEmail) / $($config.SupervisorPassword)"
    Write-Host "Mailpit: http://localhost:$($config.MailpitPort)"
    Write-Host 'Stop:    pwsh scripts/verify-stack.ps1 down'
  }
  'status' {
    Invoke-Compose ps --all
  }
  'down' {
    Invoke-Compose down --volumes --remove-orphans
    foreach ($image in $BuiltImages) {
      if (docker image ls --quiet $image) {
        & docker image rm $image | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Error "Failed to remove the image $image." }
      }
    }
    Write-Host 'The verify stack, its volumes, and its images are removed.' -ForegroundColor Green
  }
}
