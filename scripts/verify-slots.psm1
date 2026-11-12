#Requires -Version 7
# verify-slots.psm1 - the pure rules of the verify slots (scripts/verify-stack.ps1). No function calls Docker, so
# scripts/tests/verify-slots.tests.ps1 checks them without a container.
#
# A slot is a number from 1 to $MaxSlots. Slot n is the Compose project healthyfac-verify-<n>. The owner of a slot is the
# lock file of the slot (slot<n>.lock in the lock folder of the git common folder) or the working directory label that
# Compose puts on its containers. `up` claims the lock file before the build, so two parallel `up` runs get different
# slots. A crashed `up` keeps its slot until `down`.

$MaxSlots = 4
$ProjectPrefix = 'healthyfac-verify'
$GatewayPortBase = 9443
$MailpitPortBase = 8026
$GrafanaPortBase = 3000
$RabbitMqUiPortBase = 15672
$KeycloakPortBase = 8180
$DomainSuffix = 'hfverify.test'

function Get-VerifySlotNumbers { 1..$MaxSlots }

function Assert-VerifySlot {
  param([Parameter(Mandatory)][int]$Slot)
  if ($Slot -lt 1 -or $Slot -gt $MaxSlots) { throw "Slot must be from 1 to $MaxSlots." }
}

function Get-VerifySlotValues {
  param([Parameter(Mandatory)][int]$Slot)
  Assert-VerifySlot -Slot $Slot
  [pscustomobject]@{
    Slot        = $Slot
    Project     = "$ProjectPrefix-$Slot"
    Domain      = "s$Slot.$DomainSuffix"
    GatewayPort = $GatewayPortBase + $Slot
    MailpitPort = $MailpitPortBase + $Slot
    GrafanaPort = $GrafanaPortBase + $Slot
    RabbitMqUiPort = $RabbitMqUiPortBase + $Slot
    KeycloakPort = $KeycloakPortBase + $Slot
    ImagePrefix = "$ProjectPrefix-$Slot"
  }
}

function ConvertTo-NormalPath([string]$Path) {
  ($Path -replace '\\', '/').TrimEnd('/').ToLowerInvariant()
}

# Turns the lines "<project>|<working_dir>" of `docker ps --all` into one { Slot, WorkingDir } per slot project.
function ConvertTo-VerifyProjects {
  param([string[]]$Lines)
  $seen = @{}
  foreach ($line in $Lines) {
    $parts = "$line" -split '\|', 2
    if ($parts.Count -ne 2 -or $parts[0] -notmatch "^$ProjectPrefix-([1-9]\d*)$") { continue }
    $slot = [int]$Matches[1]
    if ($slot -gt $MaxSlots -or $seen.ContainsKey($slot)) { continue }
    $seen[$slot] = [pscustomobject]@{ Slot = $slot; WorkingDir = $parts[1] }
  }
  $seen.Values | Sort-Object Slot
}

function Format-SlotOwners([object[]]$Projects) {
  ($Projects | Sort-Object Slot | ForEach-Object { "slot $($_.Slot): $($_.WorkingDir)" }) -join '; '
}

# Returns the slot number for the worktree, or $null for `status` and `down` when it owns no slot.
function Select-VerifySlot {
  param(
    [Parameter(Mandatory)][ValidateSet('up', 'status', 'down')][string]$Command,
    [Parameter(Mandatory)][string]$WorkingDir,
    [object[]]$Projects = @(),
    [int]$Slot
  )
  $here = ConvertTo-NormalPath $WorkingDir
  $own = @($Projects | Where-Object { (ConvertTo-NormalPath $_.WorkingDir) -eq $here })

  if ($PSBoundParameters.ContainsKey('Slot')) {
    Assert-VerifySlot -Slot $Slot
    $owner = $Projects | Where-Object Slot -eq $Slot | Select-Object -First 1
    if (-not $owner) {
      if ($Command -eq 'up') { return $Slot }
      return $null
    }
    if ((ConvertTo-NormalPath $owner.WorkingDir) -ne $here) {
      throw "Slot $Slot belongs to another worktree: $($owner.WorkingDir)."
    }
    return $Slot
  }

  if ($own.Count -gt 0) { return [int]($own | Sort-Object Slot | Select-Object -First 1).Slot }
  if ($Command -ne 'up') { return $null }

  $taken = @($Projects | ForEach-Object { $_.Slot })
  $free = 1..$MaxSlots | Where-Object { $_ -notin $taken } | Select-Object -First 1
  if (-not $free) {
    throw "All $MaxSlots verify slots are taken. $(Format-SlotOwners $Projects). Run `down` in one of these worktrees."
  }
  [int]$free
}

# One { Slot, WorkingDir, Path } per slot<n>.lock file of the lock folder. A file that is empty or locked by a writer that
# just created it gets an empty WorkingDir: the slot counts as taken by an owner that is not known yet.
function Get-VerifySlotLocks {
  param([Parameter(Mandatory)][string]$LockFolder)
  if (-not (Test-Path -LiteralPath $LockFolder)) { return }
  foreach ($file in Get-ChildItem -LiteralPath $LockFolder -Filter 'slot*.lock' -File) {
    if ($file.Name -notmatch '^slot([1-9]\d*)\.lock$') { continue }
    $slot = [int]$Matches[1]
    if ($slot -gt $MaxSlots) { continue }
    $owner = ''
    try { $owner = ("$(Get-Content -LiteralPath $file.FullName -TotalCount 1 -ErrorAction Stop)").Trim() } catch { $owner = '' }
    [pscustomobject]@{ Slot = $slot; WorkingDir = $owner; Path = $file.FullName }
  }
}

# Creates the lock file with CreateNew, so only one caller wins a slot. The same root counts as claimed.
function Request-VerifySlotLock {
  param(
    [Parameter(Mandatory)][string]$LockFolder,
    [Parameter(Mandatory)][int]$Slot,
    [Parameter(Mandatory)][string]$WorktreeRoot
  )
  Assert-VerifySlot -Slot $Slot
  $null = New-Item -ItemType Directory -Force -Path $LockFolder
  $path = Join-Path $LockFolder "slot$Slot.lock"
  try {
    $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
  }
  catch [System.IO.IOException] {
    $existing = Get-VerifySlotLocks -LockFolder $LockFolder | Where-Object Slot -eq $Slot | Select-Object -First 1
    return [bool]($existing -and (ConvertTo-NormalPath $existing.WorkingDir) -eq (ConvertTo-NormalPath $WorktreeRoot))
  }
  try {
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($WorktreeRoot)
    $stream.Write($bytes, 0, $bytes.Length)
  }
  finally { $stream.Dispose() }
  return $true
}

# A lock is stale when its owner folder is gone and no container of its slot exists.
function Get-StaleVerifySlotLocks {
  param(
    [object[]]$Locks = @(),
    [object[]]$Projects = @(),
    [Parameter(Mandatory)][scriptblock]$FolderExists
  )
  $Locks | Where-Object {
    $lock = $_
    $lock.WorkingDir -and -not (& $FolderExists $lock.WorkingDir) -and -not ($Projects | Where-Object Slot -eq $lock.Slot)
  }
}

# The owners that Select-VerifySlot sees: a lock owner wins over a container owner of the same slot.
function Merge-VerifyOwners {
  param([object[]]$Locks = @(), [object[]]$Projects = @())
  $lockSlots = @($Locks | ForEach-Object { $_.Slot })
  @($Locks | ForEach-Object { [pscustomobject]@{ Slot = $_.Slot; WorkingDir = $_.WorkingDir } }) +
    @($Projects | Where-Object { $_.Slot -notin $lockSlots })
}

# The .env of the worktree wins; a worktree without one uses the .env of the main checkout.
function Resolve-VerifyEnvFile {
  param(
    [Parameter(Mandatory)][string]$WorktreeRoot,
    [Parameter(Mandatory)][string]$MainRoot,
    [Parameter(Mandatory)][scriptblock]$Exists
  )
  $own = Join-Path $WorktreeRoot '.env'
  $fallback = Join-Path $MainRoot '.env'
  if (& $Exists $own) { return $own }
  if (& $Exists $fallback) { return $fallback }
  throw "No .env found. Looked at $own and $fallback. Copy .env.example to one of them."
}

Export-ModuleMember -Function Get-VerifySlotLocks, Request-VerifySlotLock, Get-StaleVerifySlotLocks, Merge-VerifyOwners, Assert-VerifySlot, Get-VerifySlotNumbers, Get-VerifySlotValues, ConvertTo-VerifyProjects, Select-VerifySlot, Resolve-VerifyEnvFile
