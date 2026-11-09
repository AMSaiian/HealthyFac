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
# - Compose: slot 2 renders slot 2 names, ports, and URLs; no VERIFY_SLOT fails with the ":?" message.
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
Assert-That ($v.Project -eq 'healthyfac-verify3') 'slot 3 project'
Assert-That ($v.Domain -eq 's3.hfverify.test') 'slot 3 domain'
Assert-That ($v.GatewayPort -eq 9446) 'slot 3 gateway port is 9443 + 3'
Assert-That ($v.MailpitPort -eq 8029) 'slot 3 mailpit port is 8026 + 3'
Assert-That ($v.ImagePrefix -eq 'healthyfac-verify3') 'slot 3 image prefix'

Write-Host 'Projects'
$lines = @(
  'healthyfac-verify2|d:/repo/',
  'healthyfac-verify2|d:/repo/',
  'healthyfac|D:\Dev',
  'healthyfac-verify|D:\Old',
  'healthyfac-verify9|D:\Far',
  'healthyfac-verify10|D:\Far10',
  'healthyfac-verify1|D:\Other',
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

Write-Host 'Compose'
$composeArgs = @(
  'compose', '-p', 'healthyfac-verify2', '--project-directory', $repo,
  '--env-file', (Join-Path $repo '.env.example'), '--env-file', (Join-Path $repo '.env.verify'),
  '-f', (Join-Path $repo 'docker-compose.yml'), '-f', (Join-Path $repo 'docker\verify\docker-compose.verify.yml'),
  'config', '--format', 'json'
)
$saved = @{}
foreach ($name in 'VERIFY_SLOT', 'HEALTHYFAC_DOMAIN', 'GATEWAY_PORT', 'MAILPIT_UI_PORT') {
  $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
  $s = Get-VerifySlotValues -Slot 2
  $env:VERIFY_SLOT = '2'
  $env:HEALTHYFAC_DOMAIN = $s.Domain
  $env:GATEWAY_PORT = "$($s.GatewayPort)"
  $env:MAILPIT_UI_PORT = "$($s.MailpitPort)"
  $json = & docker @composeArgs 2>&1
  Assert-That ($LASTEXITCODE -eq 0) 'compose config renders for slot 2'
  $c = ($json -join "`n") | ConvertFrom-Json
  Assert-That ($c.name -eq 'healthyfac-verify2') 'project name'
  $names = $c.services.PSObject.Properties | ForEach-Object { $_.Value.container_name }
  Assert-That (@($names | Where-Object { $_ -notmatch '^healthyfac-verify2-' }).Count -eq 0) 'every container name starts with healthyfac-verify2-'
  $images = $c.services.PSObject.Properties | ForEach-Object { $_.Value.image } | Where-Object { $_ -match ':local$' }
  Assert-That (@($images | Where-Object { $_ -notmatch '^healthyfac-verify2/' }).Count -eq 0 -and $images.Count -ge 6) 'every built image tag starts with healthyfac-verify2/'
  Assert-That ([int]$c.services.gateway.ports[0].published -eq 9445) 'gateway port 9445'
  Assert-That ([int]$c.services.mailpit.ports[0].published -eq 8028) 'mailpit port 8028'
  Assert-That ($c.services.core.environment.Jwt__Issuer -eq 'https://auth.s2.hfverify.test:9445/realms/healthyfac' -or $c.services.core.environment.Jwt__Issuer -like 'https://auth.s2.hfverify.test:9445*') 'Jwt__Issuer uses slot 2'
  Assert-That ($c.services.keycloak.environment.KC_HOSTNAME -like 'https://auth.s2.hfverify.test:9445*') 'KC_HOSTNAME uses slot 2'
  Assert-That ($c.services.core.environment.InviteLinks__SpaBaseUrl -like 'https://app.s2.hfverify.test:9445*') 'InviteLinks__SpaBaseUrl uses slot 2'
  Assert-That ($c.services.gateway.environment.Cors__AllowedOrigins__0 -like 'https://app.s2.hfverify.test:9445*') 'Cors__AllowedOrigins__0 uses slot 2'
  Assert-That (($c.services.gateway.environment.PSObject.Properties | Where-Object { $_.Name -like '*Match__Hosts*' -and $_.Value -like '*s2.hfverify.test*' }).Count -ge 1) 'gateway Match__Hosts uses slot 2'

  Remove-Item Env:VERIFY_SLOT
  $out = & docker @composeArgs 2>&1
  Assert-That ($LASTEXITCODE -ne 0 -and ($out -join ' ') -match 'run scripts/verify-stack.ps1') 'no VERIFY_SLOT fails with the ":?" message'
}
finally {
  foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
}

if ($failures.Count -gt 0) {
  Write-Host "$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host 'All checks passed.' -ForegroundColor Green
