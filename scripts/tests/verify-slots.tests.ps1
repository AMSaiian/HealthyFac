#Requires -Version 7
# Deterministic checks of the verify slots (scripts/verify-slots.psm1, docker/verify/docker-compose.verify.yml).
# No container starts: the slot functions are pure, and the Compose cases only render `docker compose config`.
#
# Cases:
# - Slot values: project, domain, ports, and image prefix of a slot.
# - Projects: the lines of `docker ps` become the slot list; the old project and the dev project are ignored; paths
#   compare without case and with / or \ treated the same.
# - Slot choice: own slot, lowest free slot, a gap, all slots taken, -Slot free / own / foreign, down with no slot.
# - Env file: the worktree .env, the main checkout .env, or none.
# - Settings split (docker/config/README.md): each .NET service of docker-compose.yml mounts its docker/config file,
#   has no fixed "__" entry in its environment, and its file names no secret.
# - Compose: slot 2 renders slot 2 names, ports, and URLs; no VERIFY_SLOT fails with the ":?" message.
# - SPA build: spa builds the Angular development configuration by default, and production with
#   HEALTHYFAC_SPA_BUILD=production.
# Run:  pwsh scripts/tests/verify-slots.tests.ps1
# It exits 1 when a check fails.

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Import-Module (Join-Path $repo 'scripts\verify-slots.psm1') -Force
$failures = [Collections.Generic.List[string]]::new()

function Assert-That([bool] $condition, [string] $message) {
  if ($condition) { Write-Host "  ok   $message" } else {
    Write-Host "  FAIL $message" -ForegroundColor Red
    $failures.Add($message)
  }
}

function Get-Failure([scriptblock] $action) {
  try { & $action | Out-Null } catch { return $_.Exception.Message }
  return $null
}

function New-Project([int] $slot, [string] $dir) { [pscustomobject]@{ Slot = $slot; WorkingDir = $dir } }

$mine = 'D:\Repo'
$other = 'D:\Other'

Write-Host 'Slot values'
$v = Get-VerifySlotValues -Slot 3
Assert-That ($v.Project -eq 'healthyfac-verify-3') 'slot 3 project'
Assert-That ($v.Domain -eq 's3.hfverify.test') 'slot 3 domain'
Assert-That ($v.GatewayPort -eq 9446) 'slot 3 gateway port is 9443 + 3'
Assert-That ($v.MailpitPort -eq 8029) 'slot 3 mailpit port is 8026 + 3'
Assert-That ($v.GrafanaPort -eq 3003) 'slot 3 grafana port is 3000 + 3'
Assert-That ($v.RabbitMqUiPort -eq 15675) 'slot 3 rabbitmq port is 15672 + 3'
Assert-That ($v.KeycloakPort -eq 8183) 'slot 3 keycloak port is 8180 + 3'
Assert-That ($v.SimulatorPort -eq 8203) 'slot 3 simulator port is 8200 + 3'
Assert-That ($v.ImagePrefix -eq 'healthyfac-verify-3') 'slot 3 image prefix'

Write-Host 'Projects'
$lines = @(
  'healthyfac-verify-2|d:/repo/',
  'healthyfac-verify-2|d:/repo/',
  'healthyfac|D:\Dev',
  'healthyfac-verify|D:\Old',
  'healthyfac-verify-9|D:\Far',
  'healthyfac-verify-10|D:\Far10',
  'healthyfac-verify-1|D:\Other',
  ''
)
$projects = @(ConvertTo-VerifyProjects -Lines $lines)
Assert-That ($projects.Count -eq 2) 'the old project, the dev project, an out-of-range slot, and blanks are ignored'
Assert-That (($projects | Where-Object Slot -eq 2).WorkingDir -eq 'd:/repo/') 'the working dir is kept'

Write-Host 'Slot range'
$max = (Get-VerifySlotNumbers | Measure-Object -Maximum).Maximum
foreach ($bad in 0, ($max + 1)) {
  $m1 = Get-Failure { Get-VerifySlotValues -Slot $bad }
  $m2 = Get-Failure { Select-VerifySlot -Command up -WorkingDir $mine -Projects @() -Slot $bad }
  Assert-That ($m1 -eq "Slot must be from 1 to $max." -and $m2 -eq $m1) "slot $bad fails with the range message"
}
Assert-That ((Get-VerifySlotValues -Slot $max).Slot -eq $max -and (Select-VerifySlot -Command up -WorkingDir $mine -Projects @() -Slot $max) -eq $max) "slot $max passes"

Write-Host 'Slot choice'
$p = @(New-Project 2 'd:/repo/')
Assert-That ((Select-VerifySlot -Command up -WorkingDir $mine -Projects $p) -eq 2) 'own slot reused (path forms D:\Repo and d:/repo/)'
Assert-That ((Select-VerifySlot -Command up -WorkingDir $other -Projects $p) -eq 1) 'a gap: slot 1 free, slot 2 taken gives slot 1'
$p = @(New-Project 1 $other)
Assert-That ((Select-VerifySlot -Command up -WorkingDir $mine -Projects @()) -eq 1) 'no project gives slot 1'
Assert-That ((Select-VerifySlot -Command up -WorkingDir $mine -Projects $p) -eq 2) 'lowest free slot after slot 1'
$all = Get-VerifySlotNumbers | ForEach-Object { New-Project $_ "D:\Wt$_" }
$message = Get-Failure { Select-VerifySlot -Command up -WorkingDir $mine -Projects $all }
Assert-That ($message -match 'D:\\Wt1' -and $message -match 'D:\\Wt4') 'all slots taken: the error lists the owners'
Assert-That ((Select-VerifySlot -Command up -WorkingDir $mine -Projects $p -Slot 3) -eq 3) '-Slot free'
$p = @(New-Project 3 'd:/repo')
Assert-That ((Select-VerifySlot -Command down -WorkingDir $mine -Projects $p -Slot 3) -eq 3) '-Slot own'
$p = @(New-Project 3 $other)
$message = Get-Failure { Select-VerifySlot -Command up -WorkingDir $mine -Projects $p -Slot 3 }
Assert-That ($message -match 'D:\\Other') '-Slot foreign fails for up and names the owner'
Assert-That ($null -ne (Get-Failure { Select-VerifySlot -Command down -WorkingDir $mine -Projects $p -Slot 3 })) '-Slot foreign fails for down'
Assert-That ($null -ne (Get-Failure { Select-VerifySlot -Command status -WorkingDir $mine -Projects $p -Slot 3 })) '-Slot foreign fails for status'
Assert-That ($null -eq (Select-VerifySlot -Command down -WorkingDir $mine -Projects $p)) 'down with no own slot returns $null'
Assert-That ($null -eq (Select-VerifySlot -Command status -WorkingDir $mine -Projects $p)) 'status with no own slot returns $null'
Assert-That ($null -eq (Select-VerifySlot -Command down -WorkingDir $mine -Projects @() -Slot 2)) 'down -Slot free returns $null'

Write-Host 'Lock files'
$lockFolder = Join-Path ([IO.Path]::GetTempPath()) "hf-locks-$([guid]::NewGuid().ToString('N'))"
try {
  Assert-That ($null -eq (Get-VerifySlotLocks -LockFolder $lockFolder)) 'no lock folder gives no locks'
  Assert-That ((Request-VerifySlotLock -LockFolder $lockFolder -Slot 1 -WorktreeRoot $mine) -eq $true) 'the first claim of slot 1 wins'
  Assert-That ((Request-VerifySlotLock -LockFolder $lockFolder -Slot 1 -WorktreeRoot $other) -eq $false) 'a claim of slot 1 from another root fails'
  Assert-That ((Request-VerifySlotLock -LockFolder $lockFolder -Slot 1 -WorktreeRoot 'd:/repo/') -eq $true) 'a claim from the same root counts as claimed'
  $locks = @(Get-VerifySlotLocks -LockFolder $lockFolder)
  Assert-That ($locks.Count -eq 1 -and $locks[0].Slot -eq 1 -and $locks[0].WorkingDir -eq $mine) 'the lock file holds the first root'
  $owners = @(Merge-VerifyOwners -Locks $locks -Projects @())
  Assert-That ((Select-VerifySlot -Command up -WorkingDir $other -Projects $owners) -eq 2) 'the choice with a lock of another root on slot 1 gives slot 2'
  Assert-That ((Select-VerifySlot -Command up -WorkingDir $mine -Projects $owners) -eq 1) 'the lock owner reuses its slot'
  $merged = @(Merge-VerifyOwners -Locks $locks -Projects @(New-Project 1 $other))
  Assert-That ($merged.Count -eq 1 -and $merged[0].WorkingDir -eq $mine) 'a lock owner wins over a container owner'
  Set-Content -LiteralPath (Join-Path $lockFolder 'slot2.lock') -Value '' -NoNewline
  Assert-That ($null -eq (Get-StaleVerifySlotLocks -Locks @(Get-VerifySlotLocks -LockFolder $lockFolder | Where-Object Slot -eq 2) -Projects @() -FolderExists { param($d) $false })) 'an empty lock file is never stale'
}
finally { Remove-Item -LiteralPath $lockFolder -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host 'Stale locks'
$lockA = [pscustomobject]@{ Slot = 1; WorkingDir = 'D:\Gone'; Path = 'a' }
$lockB = [pscustomobject]@{ Slot = 2; WorkingDir = 'D:\Here'; Path = 'b' }
$lockC = [pscustomobject]@{ Slot = 3; WorkingDir = 'D:\Gone3'; Path = 'c' }
$exists = { param($d) $d -eq 'D:\Here' }
$stale = @(Get-StaleVerifySlotLocks -Locks @($lockA, $lockB, $lockC) -Projects @(New-Project 3 'D:\Gone3') -FolderExists $exists)
Assert-That ($stale.Count -eq 1 -and $stale[0].Slot -eq 1) 'a lock with a missing folder and no container is stale; a lock with a container or an existing folder is kept'

Write-Host 'Env file'
$main = 'D:\Main'
$tree = 'D:\Tree'
$both = { param($path) $true }
$none = { param($path) $false }
$onlyMain = { param($path) $path -like 'D:\Main*' }
Assert-That ((Resolve-VerifyEnvFile -WorktreeRoot $tree -MainRoot $main -Exists $both) -eq 'D:\Tree\.env') 'the worktree .env wins'
Assert-That ((Resolve-VerifyEnvFile -WorktreeRoot $tree -MainRoot $main -Exists $onlyMain) -eq 'D:\Main\.env') 'fallback to the main checkout .env'
$message = Get-Failure { Resolve-VerifyEnvFile -WorktreeRoot $tree -MainRoot $main -Exists $none }
Assert-That ($message -match 'D:\\Tree\\\.env' -and $message -match 'D:\\Main\\\.env') 'no .env: the error names both paths'

Write-Host 'Seed sign-in'
$signInDir = Join-Path ([IO.Path]::GetTempPath()) ("verify-sign-in-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $signInDir | Out-Null
$signInFile = Join-Path $signInDir 'company.json'
Set-Content -Path $signInFile -Value '{ "password": "Test-Password-1!", "people": [ { "key": "nadiia", "email": "sup@test.example", "role": "supervisor" }, { "key": "andrii", "email": "work@test.example", "role": "worker" } ] }'
$signIn = Get-SeedSignIn -Path $signInFile
Assert-That ($signIn.SupervisorEmail -eq 'sup@test.example') 'the sign-in reader returns the supervisor email (nadiia)'
Assert-That ($signIn.WorkerEmail -eq 'work@test.example') 'the sign-in reader returns the worker email (andrii)'
Assert-That ($signIn.Password -eq 'Test-Password-1!') 'the sign-in reader returns the password'
$missing = Join-Path $signInDir 'missing.json'
$message = Get-Failure { Get-SeedSignIn -Path $missing }
Assert-That ($message -like "*$missing*") 'a missing company.json fails with its path'
Remove-Item -Recurse -Force $signInDir
$committed = Get-SeedSignIn -Path (Join-Path $repo 'backend\seed\demo\company.json')
Assert-That ($committed.SupervisorEmail -and $committed.WorkerEmail -and $committed.Password) 'the sign-in reader reads the committed company.json'

Write-Host 'Settings split'
# The check reads the raw Compose file, because `docker compose config` replaces every variable, and a fixed value
# then looks like a value from .env. A .NET service is a service whose Dockerfile is a HealthyFac.<Name>.Web one or
# the one of the machine simulator or the seed job.
$composeLines = Get-Content (Join-Path $repo 'docker-compose.yml')
$blocks = [ordered]@{}
$service = $null
foreach ($line in $composeLines) {
  $start = [regex]::Match($line, '^  ([a-z0-9-]+):\s*$')
  if ($start.Success) { $service = $start.Groups[1].Value; $blocks[$service] = [Collections.Generic.List[string]]::new(); continue }
  if ($line -match '^\S') { $service = $null }
  if ($service) { $blocks[$service].Add($line) }
}
$dotnetServices = @($blocks.Keys | Where-Object { $blocks[$_] -match 'dockerfile:\s*src/(\w+/HealthyFac\.\w+\.Web|Tools/HealthyFac\.(MachineSimulator|Seeder))/Dockerfile' })
Assert-That ($dotnetServices.Count -ge 8) "docker-compose.yml has the .NET services ($($dotnetServices -join ', '))"
$SecretName = '(?i)(password|secret|apikey|^key)$'
foreach ($name in $dotnetServices) {
  $block = $blocks[$name]
  $fixed = [Collections.Generic.List[string]]::new()
  $isInEnvironment = $false
  for ($i = 0; $i -lt $block.Count; $i++) {
    if ($block[$i] -match '^    environment:') { $isInEnvironment = $true; continue }
    if ($block[$i] -match '^    \S') { $isInEnvironment = $false }
    $entry = [regex]::Match($block[$i], '^      ([A-Za-z0-9_-]+__[A-Za-z0-9_-]+):\s*(.*)$')
    if (-not $isInEnvironment -or -not $entry.Success) { continue }
    $value = $entry.Groups[2].Value.Trim()
    if ($value -in '>-', '>', '|') { $value = $block[$i + 1] }
    if ($value -notmatch '\$\{') { $fixed.Add($entry.Groups[1].Value) }
  }
  Assert-That ($fixed.Count -eq 0) "$name has no fixed '__' entry in its environment ($($fixed -join ', '))"

  if ($name -eq 'seeder') {
    foreach ($hostName in 'core', 'telemetry', 'document', 'insights', 'auth') {
      $hostMount = "./docker/config/$hostName.json:/app/services/$hostName/appsettings.Deployment.json:ro"
      $hostFile = Join-Path $repo "docker\config\$hostName.json"
      Assert-That ([bool]($block -match [regex]::Escape($hostMount)) -and (Test-Path $hostFile)) "seeder mounts docker/config/$hostName.json for the $hostName host"
    }
    Assert-That ([bool]($block -match [regex]::Escape('./backend/seed/demo:/app/seed/demo:ro'))) 'seeder mounts backend/seed/demo read-only'
    continue
  }

  $mount = "./docker/config/$name.json:/app/appsettings.Deployment.json:ro"
  $configPath = Join-Path $repo "docker\config\$name.json"
  Assert-That ([bool]($block -match [regex]::Escape($mount)) -and (Test-Path $configPath)) "$name mounts docker/config/$name.json"
  if (-not (Test-Path $configPath)) { continue }

  $secrets = [Collections.Generic.List[string]]::new()
  $pending = [Collections.Generic.Stack[object]]::new()
  $pending.Push(@('', (Get-Content $configPath -Raw | ConvertFrom-Json)))
  while ($pending.Count -gt 0) {
    $path, $node = $pending.Pop()
    if ($node -is [pscustomobject]) {
      foreach ($property in $node.PSObject.Properties) {
        if ($property.Name -match $SecretName) { $secrets.Add("$path$($property.Name)") }
        $pending.Push(@("$path$($property.Name):", $property.Value))
      }
    } elseif ($node -is [array]) {
      foreach ($item in $node) { $pending.Push(@($path, $item)) }
    } elseif ("$node" -match '(?i)password\s*=') { $secrets.Add("$path (a password in the value)") }
  }
  Assert-That ($secrets.Count -eq 0) "docker/config/$name.json names no secret ($($secrets -join ', '))"
}

foreach ($name in $blocks.Keys) {
  $old = @($blocks[$name] | Where-Object { $_ -match 'Seeding__|HEALTHYFAC_TEST_' })
  Assert-That ($old.Count -eq 0) "$name has no Seeding__ or HEALTHYFAC_TEST_ entry"
}
foreach ($name in 'core', 'telemetry', 'document', 'insights', 'auth') {
  $blockText = $blocks[$name] -join "`n"
  Assert-That ($blockText -match '(?m)^      seeder:\s*\n        condition: service_completed_successfully') "$name waits for the seeder (service_completed_successfully)"
}
Assert-That ($dotnetServices -contains 'seeder') 'docker-compose.yml has the seeder service'

Write-Host 'gRPC ports'
$grpcSettings = @(Get-ChildItem (Join-Path $repo 'backend\src') -Recurse -Filter appsettings.json |
    Where-Object { $_.FullName -match '\\HealthyFac\.\w+\.Web\\appsettings\.json$' })
foreach ($settingsFile in $grpcSettings) {
  $grpcPort = (Get-Content $settingsFile.FullName -Raw | ConvertFrom-Json).Grpc.Port
  if (-not $grpcPort) { continue }
  $serviceName = Split-Path $settingsFile.DirectoryName -Leaf
  $dockerfileText = Get-Content (Join-Path $settingsFile.DirectoryName 'Dockerfile') -Raw
  Assert-That ([bool]($dockerfileText -match "ASPNETCORE_HTTP_PORTS=[\d;]*\b$grpcPort\b")) "$serviceName lists the gRPC port $grpcPort in ASPNETCORE_HTTP_PORTS"
  Assert-That ([bool]($dockerfileText -match "(?m)^EXPOSE[\d ]*\b$grpcPort\b")) "$serviceName exposes the gRPC port $grpcPort"
}

Write-Host 'Compose'
$composeArgs = @(
  'compose', '-p', 'healthyfac-verify-2', '--profile', 'simulator', '--project-directory', $repo,
  '--env-file', (Join-Path $repo '.env.example'), '--env-file', (Join-Path $repo '.env.verify'),
  '-f', (Join-Path $repo 'docker-compose.yml'), '-f', (Join-Path $repo 'docker\verify\docker-compose.verify.yml'),
  'config', '--format', 'json'
)
$saved = @{}
foreach ($name in 'VERIFY_SLOT', 'HEALTHYFAC_DOMAIN', 'GATEWAY_PORT', 'MAILPIT_UI_PORT', 'GRAFANA_PORT', 'RABBITMQ_MANAGEMENT_PORT', 'KEYCLOAK_PORT', 'SIMULATOR_PORT', 'HEALTHYFAC_SPA_BUILD') {
  $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
  [Environment]::SetEnvironmentVariable('HEALTHYFAC_SPA_BUILD', $null)
  $s = Get-VerifySlotValues -Slot 2
  $env:VERIFY_SLOT = '2'
  $env:HEALTHYFAC_DOMAIN = $s.Domain
  $env:GATEWAY_PORT = "$($s.GatewayPort)"
  $env:MAILPIT_UI_PORT = "$($s.MailpitPort)"
  $env:GRAFANA_PORT = "$($s.GrafanaPort)"
  $env:RABBITMQ_MANAGEMENT_PORT = "$($s.RabbitMqUiPort)"
  $env:KEYCLOAK_PORT = "$($s.KeycloakPort)"
  $env:SIMULATOR_PORT = "$($s.SimulatorPort)"
  $json = & docker @composeArgs 2>&1
  Assert-That ($LASTEXITCODE -eq 0) 'compose config renders for slot 2'
  $c = ($json -join "`n") | ConvertFrom-Json
  Assert-That ($c.name -eq 'healthyfac-verify-2') 'project name'
  $names = $c.services.PSObject.Properties | ForEach-Object { $_.Value.container_name }
  Assert-That (@($names | Where-Object { $_ -notmatch '^healthyfac-verify-2-' }).Count -eq 0) 'every container name starts with healthyfac-verify-2-'
  $images = $c.services.PSObject.Properties | ForEach-Object { $_.Value.image } | Where-Object { $_ -match ':local$' }
  Assert-That (@($images | Where-Object { $_ -notmatch '^healthyfac-verify-2/' }).Count -eq 0 -and $images.Count -ge 6) 'every built image tag starts with healthyfac-verify-2/'
  Assert-That ([int]$c.services.gateway.ports[0].published -eq 9445) 'gateway port 9445'
  Assert-That ([int]$c.services.mailpit.ports[0].published -eq 8028) 'mailpit port 8028'
  Assert-That ([int]$c.services.lgtm.ports[0].published -eq 3002 -and @($c.services.lgtm.ports).Count -eq 1) 'grafana port 3002 only'
  Assert-That ([int]$c.services.rabbitmq.ports[0].published -eq 15674 -and @($c.services.rabbitmq.ports).Count -eq 1) 'rabbitmq management port 15674 only'
  Assert-That ([int]$c.services.keycloak.ports[0].published -eq 8182 -and $c.services.keycloak.environment.KC_HOSTNAME_ADMIN -eq 'http://localhost:8182') 'keycloak admin port 8182 matches KC_HOSTNAME_ADMIN'
  Assert-That ([int]$c.services.simulator.ports[0].published -eq 8202 -and @($c.services.simulator.ports).Count -eq 1 -and $c.services.simulator.container_name -eq 'healthyfac-verify-2-simulator') 'simulator port 8202 only, in the slot 2 container'
  Assert-That ($c.services.simulator.image -eq 'healthyfac-verify-2/simulator:local') 'simulator image tag uses slot 2'
  Assert-That ($c.services.core.environment.Jwt__Issuer -eq 'https://auth.s2.hfverify.test:9445/realms/healthyfac' -or $c.services.core.environment.Jwt__Issuer -like 'https://auth.s2.hfverify.test:9445*') 'Jwt__Issuer uses slot 2'
  Assert-That ($c.services.keycloak.environment.KC_HOSTNAME -like 'https://auth.s2.hfverify.test:9445*') 'KC_HOSTNAME uses slot 2'
  Assert-That ($c.services.core.environment.InviteLinks__SpaBaseUrl -like 'https://app.s2.hfverify.test:9445*') 'InviteLinks__SpaBaseUrl uses slot 2'
  Assert-That ($c.services.gateway.environment.Cors__AllowedOrigins__0 -like 'https://app.s2.hfverify.test:9445*') 'Cors__AllowedOrigins__0 uses slot 2'
  Assert-That (($c.services.gateway.environment.PSObject.Properties | Where-Object { $_.Name -like '*Match__Hosts*' -and $_.Value -like '*s2.hfverify.test*' }).Count -ge 1) 'gateway Match__Hosts uses slot 2'
  Assert-That ($c.services.spa.build.args.SPA_BUILD_CONFIGURATION -eq 'development') 'spa builds the development configuration without HEALTHYFAC_SPA_BUILD'

  $env:HEALTHYFAC_SPA_BUILD = 'production'
  $production = (& docker @composeArgs 2>&1) -join "`n" | ConvertFrom-Json
  Assert-That ($production.services.spa.build.args.SPA_BUILD_CONFIGURATION -eq 'production') 'spa builds the production configuration with HEALTHYFAC_SPA_BUILD=production'
  [Environment]::SetEnvironmentVariable('HEALTHYFAC_SPA_BUILD', $null)

  Remove-Item Env:VERIFY_SLOT
  $out = & docker @composeArgs 2>&1
  Assert-That ($LASTEXITCODE -ne 0 -and ($out -join ' ') -match 'run scripts/verify-stack.ps1') 'no VERIFY_SLOT fails with the ":?" message'
}
finally {
  foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
}

Write-Host 'Telemetry switch'
$withTelemetry = @(Get-VerifyUpComposeCalls -Telemetry)
Assert-That ($withTelemetry.Count -eq 2) '-Telemetry gives 2 compose calls'
Assert-That (($withTelemetry[0] -join ' ') -eq 'run --rm --build seeder --telemetry') '-Telemetry runs the seeder with --telemetry first'
Assert-That (($withTelemetry[1] -join ' ') -eq 'up -d --build') '-Telemetry then starts the stack with up -d --build'
$withoutTelemetry = @(Get-VerifyUpComposeCalls)
Assert-That ($withoutTelemetry.Count -eq 1 -and ($withoutTelemetry[0] -join ' ') -eq 'up -d --build') 'without -Telemetry the only compose call is up -d --build'
Assert-That (-not ($withoutTelemetry | Where-Object { $_ -contains 'seeder' })) 'without -Telemetry no seeder run'
Assert-That ((Get-VerifyTelemetryUsageError -Command 'up' -IsTelemetry $true) -eq $null) '-Telemetry with up has no usage error'
Assert-That ((Get-VerifyTelemetryUsageError -Command 'stop' -IsTelemetry $false) -eq $null) 'stop without -Telemetry has no usage error'
foreach ($other in 'status', 'stop', 'reset', 'down') {
  Assert-That ((Get-VerifyTelemetryUsageError -Command $other -IsTelemetry $true) -match 'only with up') "-Telemetry with $other gives the usage message"
}
$scriptOutput = & pwsh -NoProfile -File (Join-Path $repo 'scripts\verify-stack.ps1') stop -Telemetry 2>&1
Assert-That ($LASTEXITCODE -ne 0 -and ($scriptOutput -join ' ') -match 'only with up') 'verify-stack.ps1 stop -Telemetry fails with the usage message'

if ($failures.Count -gt 0) {
  Write-Host "$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host 'All checks passed.' -ForegroundColor Green
