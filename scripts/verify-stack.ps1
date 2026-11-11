# verify-stack.ps1 - starts, shows, and removes the isolated verify stack, so an agent (or you) can check the UI in
# a browser next to the running dev stack and next to the verify stacks of other worktrees. It never touches the dev
# project "healthyfac".
#
# Slots: every worktree owns one slot (1 to the slot count of verify-slots.psm1) while its stack exists. Slot n is the Compose project healthyfac-verify<n>
# with the domain s<n>.hfverify.test, the gateway port 9443 + n, and the Mailpit port 8026 + n. `up` takes the slot of
# the worktree, else the lowest free slot. `status` and `down` act on the slot of the worktree only. -Slot names a
# slot; the script refuses a slot that another worktree owns. The owner is the worktree root in the lock file
# slot<n>.lock (folder healthyfac-verify-slots in the git common folder), else the working directory label of the project.
# `up` creates the lock file before the build, so two parallel `up` runs get different slots; `down` removes it. A crashed
# `up` keeps its slot until `down`. A lock file whose folder is gone and whose slot has no container is removed.
# The rules are in scripts/verify-slots.psm1 and are checked by scripts/tests/verify-slots.tests.ps1.
#
# Usage, from anywhere inside the repo or a worktree:
#   pwsh scripts/verify-stack.ps1 up [-Slot n]   builds and starts the stack, waits until it is ready, prints the sign-in
#   pwsh scripts/verify-stack.ps1 status         lists the containers of the stack
#   pwsh scripts/verify-stack.ps1 down [-Slot n] removes the containers, the volumes, and the images of the stack
#
# Configuration: Compose reads .env (the secrets, shared with the dev stack) and then .env.verify (the shared
# overrides). The script sets VERIFY_SLOT, HEALTHYFAC_DOMAIN, GATEWAY_PORT, and MAILPIT_UI_PORT in the process
# environment. docker/verify/docker-compose.verify.yml renames the containers and images and removes every other host
# port. A worktree without a .env uses the .env of the main checkout.
#
# One-time setup:
# - Add this line to C:\Windows\System32\drivers\etc\hosts as an administrator (the names of every slot):
#     127.0.0.1 <api|auth|app>.s<n>.hfverify.test for every slot n (a missing name makes `up` print the full line)
# - mkcert must be installed (docker/certs/README.md). The script creates the wildcard certificate for
#   *.s<n>.hfverify.test for every slot when it is missing or lacks a slot name.
# - .env must exist in the worktree or in the main checkout (copy .env.example).
#Requires -Version 7.4
param(
  [Parameter(Mandatory = $true, Position = 0)]
  [ValidateSet('up', 'status', 'down')]
  [string]$Command,

  [int]$Slot
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'verify-slots.psm1') -Force

$DevProject = 'healthyfac'
$OldProject = 'healthyfac-verify'
$ReadyTimeout = [TimeSpan]::FromMinutes(15)
$AppTimeout = [TimeSpan]::FromMinutes(2)
$PollInterval = [TimeSpan]::FromSeconds(5)
$MinFreeMemoryGiB = 3
$ReadyServices = @('core', 'telemetry', 'document', 'insights', 'auth', 'spa', 'gateway')
$RealmJob = 'keycloak-config'
$BuiltServices = @('core', 'telemetry', 'document', 'insights', 'auth', 'spa', 'gateway', 'keycloak')
$SlotNumbers = Get-VerifySlotNumbers
$HostsFile = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'

$repo = git rev-parse --show-toplevel 2>$null
if (-not $repo) { Write-Error 'Run this script from inside the repo.' }
$repo = $repo -replace '/', '\'
$commonDir = (git rev-parse --path-format=absolute --git-common-dir) -replace '/', '\'
$mainRepo = Split-Path $commonDir -Parent
$envFile = Resolve-VerifyEnvFile -WorktreeRoot $repo -MainRoot $mainRepo -Exists { param($path) Test-Path $path }
$verifyEnvFile = Join-Path $repo '.env.verify'
$certFile = Join-Path $repo 'docker\certs\hfverify.test.pem'
$keyFile = Join-Path $repo 'docker\certs\hfverify.test-key.pem'

if (-not (Test-Path $verifyEnvFile)) { Write-Error "Missing $verifyEnvFile. See the header of this script." }

$lockFolder = Join-Path $commonDir 'healthyfac-verify-slots'

function Get-SlotOwners {
  $projectLines = docker ps --all --filter label=com.docker.compose.project `
    --format '{{.Label "com.docker.compose.project"}}|{{.Label "com.docker.compose.project.working_dir"}}'
  if ($LASTEXITCODE -ne 0) { Write-Error 'docker ps failed. Start Docker and run the command again.' }
  $projects = @(ConvertTo-VerifyProjects -Lines $projectLines)
  $locks = @(Get-VerifySlotLocks -LockFolder $lockFolder)
  $stale = @(Get-StaleVerifySlotLocks -Locks $locks -Projects $projects -FolderExists { param($path) Test-Path -LiteralPath $path })
  foreach ($lock in $stale) {
    Write-Host "Removing the stale lock file $($lock.Path) (the folder $($lock.WorkingDir) does not exist)."
    Remove-Item -LiteralPath $lock.Path -Force
  }
  $locks = @($locks | Where-Object { $_.Path -notin @($stale | ForEach-Object { $_.Path }) })
  return @(Merge-VerifyOwners -Locks $locks -Projects $projects)
}

$hasSlot = $PSBoundParameters.ContainsKey('Slot')

function Select-OwnSlot {
  $selectArgs = @{ Command = $Command; WorkingDir = $repo; Projects = (Get-SlotOwners) }
  if ($hasSlot) { $selectArgs.Slot = $Slot }
  return Select-VerifySlot @selectArgs
}

# `up` claims the lock file before the build, so two parallel runs never share a slot. A lost claim chooses again.
$slotNumber = $null
for ($attempt = 1; $attempt -le $SlotNumbers.Count; $attempt++) {
  $slotNumber = Select-OwnSlot
  if ($Command -ne 'up' -or $null -eq $slotNumber) { break }
  if (Request-VerifySlotLock -LockFolder $lockFolder -Slot $slotNumber -WorktreeRoot $repo) { break }
  $slotNumber = $null
}
if ($Command -eq 'up' -and $null -eq $slotNumber) {
  Write-Error 'No verify slot could be claimed. Another worktree took each free slot. Run the command again.'
}

if ($null -eq $slotNumber) {
  Write-Host 'No verify stack of this worktree runs.'
  exit 0
}

$values = Get-VerifySlotValues -Slot $slotNumber
$Project = $values.Project
if ($Project -eq $DevProject) { Write-Error "Refusing to run: the project name equals the dev project '$DevProject'." }

$env:VERIFY_SLOT = "$slotNumber"
$env:HEALTHYFAC_DOMAIN = $values.Domain
$env:GATEWAY_PORT = "$($values.GatewayPort)"
$env:MAILPIT_UI_PORT = "$($values.MailpitPort)"

$BuiltImages = $BuiltServices | ForEach-Object { "$($values.ImagePrefix)/${_}:local" }

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
    GatewayPort     = [int]($config.services.gateway.ports | Select-Object -First 1).published
    MailpitPort     = [int]($config.services.mailpit.ports | Select-Object -First 1).published
    SupervisorEmail = $config.services.core.environment.Seeding__DevelopmentData__TestTenant__SupervisorEmail
    SupervisorPassword = $config.services.'keycloak-config'.environment.HEALTHYFAC_TEST_SUPERVISOR_PASSWORD
  }
}

function Get-SlotHostNames([int[]]$slots) {
  foreach ($n in $slots) {
    $domain = (Get-VerifySlotValues -Slot $n).Domain
    foreach ($prefix in 'api', 'auth', 'app') { "$prefix.$domain" }
  }
}

function Assert-HostNames([string[]]$hostNames) {
  $hosts = Get-Content $HostsFile -ErrorAction SilentlyContinue
  $missing = $hostNames | Where-Object {
    $name = $_
    -not ($hosts | Where-Object { $_ -match "^\s*127\.0\.0\.1\s.*\b$([regex]::Escape($name))\b" })
  }
  if ($missing) {
    Write-Error ("The hosts file lacks: $($missing -join ', '). Add this line (the names of every slot) to $HostsFile as an administrator:`n" +
      "  127.0.0.1 $((Get-SlotHostNames $SlotNumbers) -join ' ')")
  }
}

function Get-CertificateDnsNames {
  if (-not ((Test-Path $certFile) -and (Test-Path $keyFile))) { return @() }
  $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPemFile($certFile, $keyFile)
  try {
    $extension = $certificate.Extensions |
      Where-Object { $_ -is [System.Security.Cryptography.X509Certificates.X509SubjectAlternativeNameExtension] } |
      Select-Object -First 1
    if (-not $extension) { return @() }
    return @($extension.EnumerateDnsNames())
  }
  finally { $certificate.Dispose() }
}

function Assert-Certificate {
  $wanted = $SlotNumbers | ForEach-Object { "*.$((Get-VerifySlotValues -Slot $_).Domain)" }
  $present = @(Get-CertificateDnsNames)
  if (-not ($wanted | Where-Object { $_ -notin $present })) { return }
  if (-not (Get-Command mkcert -ErrorAction SilentlyContinue)) {
    Write-Error 'The verify certificate is missing or lacks a slot name, and mkcert is not installed. See docker/certs/README.md.'
  }
  Write-Host "Creating the verify certificate for: $($wanted -join ', ')"
  & mkcert -cert-file $certFile -key-file $keyFile @wanted
  if ($LASTEXITCODE -ne 0) { Write-Error 'mkcert failed to create the verify certificate.' }
}

function Assert-PortFree([int]$port, [string]$name) {
  $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
  if (-not $listener) { return }
  $ownContainer = docker ps --filter "label=com.docker.compose.project=$Project" --format '{{.Ports}}' |
    Where-Object { $_ -match ":$port->" }
  if (-not $ownContainer) { Write-Error "Port $port ($name) is in use by another program." }
}

function ConvertTo-Bytes([string]$text) {
  if ($text -notmatch '^\s*([\d.]+)\s*([kKmMgGtT]?i?[bB])\s*$') { return 0 }
  $number = [double]::Parse($Matches[1], [Globalization.CultureInfo]::InvariantCulture)
  $factor = switch -Regex ($Matches[2].ToLowerInvariant()) {
    '^b$' { 1 }
    '^k' { 1KB }
    '^m' { 1MB }
    '^g' { 1GB }
    '^t' { 1TB }
  }
  return $number * $factor
}

function Show-MemoryWarning {
  $total = [double](docker info --format '{{.MemTotal}}')
  $used = (docker stats --no-stream --format '{{.MemUsage}}' | ForEach-Object { ConvertTo-Bytes (($_ -split '/')[0]) } |
    Measure-Object -Sum).Sum
  $freeGiB = ($total - $used) / 1GB
  if ($freeGiB -lt $MinFreeMemoryGiB) {
    Write-Host ("Warning: only {0:N1} GiB of Docker memory is free (less than $MinFreeMemoryGiB GiB). The stack may start slowly or fail." -f $freeGiB) -ForegroundColor Yellow
  }
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
    Assert-HostNames (Get-SlotHostNames @($slotNumber))
    Assert-Certificate
    Assert-PortFree $config.GatewayPort 'the gateway port'
    Assert-PortFree $config.MailpitPort 'the Mailpit port'
    if (docker ps --all --filter "label=com.docker.compose.project=$OldProject" --quiet) {
      Write-Host "A container of the old project $OldProject exists. Remove it with: docker compose -p $OldProject down --volumes" -ForegroundColor Yellow
    }
    Show-MemoryWarning
    Invoke-Compose up -d --build
    Wait-Ready
    Wait-AppAnswers $config.SpaUrl
    # The test supervisor is a development-only account of the seeded test tenant; the agent needs it to sign in.
    Write-Host ''
    Write-Host "The verify stack is ready (slot $slotNumber)." -ForegroundColor Green
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
    Remove-Item -LiteralPath (Join-Path $lockFolder "slot$slotNumber.lock") -Force -ErrorAction SilentlyContinue
    Write-Host "The verify stack of slot $slotNumber, its volumes, its images, and its lock file are removed." -ForegroundColor Green
  }
}
