#Requires -Version 7
# agent-chrome.ps1 - the Chrome that the agents use for the browser checks (Claude in Chrome), beside Artur's own Chrome.
# Usage: pwsh scripts/agent-chrome.ps1 <start|restore|status> [-EnvFile <path>]
#
# Why a second Chrome: Chrome applies its command-line switches only when it starts a new browser process. A start into
# the user data folder of a running Chrome joins that process and ignores them. So the agent Chrome has its own user
# data folder (HEALTHYFAC_AGENT_CHROME_DIR) and runs as its own process.
# Switches (start):
# - --force-device-scale-factor=1: draws at the real screen resolution, so a 1920 x 1080 window fits on a scaled screen.
# - --disable-renderer-backgrounding, --disable-backgrounding-occluded-windows,
#   --disable-features=CalculateNativeWinOcclusion: a covered window keeps drawing.
# Measured on 2026-11-24: screenshots work while the window is covered and while the display is off. A minimized
# window draws no frames, so every screenshot times out after 30 s, also with these switches. The page reports
# "visible" also then, so a page script cannot see it; restore finds it.
#
# Commands:
# - start: copies HEALTHYFAC_AGENT_CHROME_SOURCE_PROFILE (a profile folder of the default Chrome user data, e.g.
#   "Profile 2", with the Claude in Chrome extension) once into HEALTHYFAC_AGENT_CHROME_DIR under the same folder name,
#   then starts the agent Chrome on it. Without the source setting it starts the profile "Default" of that folder:
#   install the extension in it once.
# - restore: shows each minimized window of the agent Chrome again, without the focus. Exit 1 when it does not run.
# - status: exit 0 when the agent Chrome runs, else 1.
# Settings: .env of the worktree, else of the main checkout (Resolve-VerifyEnvFile); -EnvFile overrides both.
param(
  [Parameter(Mandatory = $true, Position = 0)]
  [ValidateSet('start', 'restore', 'status')]
  [string]$Command,

  [string]$EnvFile,

  [string]$ChromePath = (Join-Path $env:ProgramFiles 'Google\Chrome\Application\chrome.exe')
)

$ErrorActionPreference = 'Stop'
$ShowNoActivate = 4

function Get-EnvValue([string] $path, [string] $name) {
  $line = Get-Content -LiteralPath $path | Where-Object { $_ -match "^\s*$name\s*=" } | Select-Object -Last 1
  if (-not $line) { return $null }
  $value = ($line -split '=', 2)[1].Trim().Trim('"')
  if ($value) { return $value } else { return $null }
}

function Get-AgentBrowserProcess([string] $userDataDir) {
  Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine -notmatch '--type=' -and
      $_.CommandLine.Contains($userDataDir, [StringComparison]::OrdinalIgnoreCase) } |
    Select-Object -First 1
}

if (-not $EnvFile) {
  Import-Module (Join-Path $PSScriptRoot 'verify-slots.psm1') -Force
  $repo = (git -C $PSScriptRoot rev-parse --show-toplevel) -replace '/', '\'
  $mainRepo = Split-Path ((git -C $repo rev-parse --path-format=absolute --git-common-dir) -replace '/', '\') -Parent
  $EnvFile = Resolve-VerifyEnvFile -WorktreeRoot $repo -MainRoot $mainRepo -Exists { param($path) Test-Path $path }
}

$userDataDir = Get-EnvValue $EnvFile 'HEALTHYFAC_AGENT_CHROME_DIR'
if (-not $userDataDir) {
  Write-Host "Set HEALTHYFAC_AGENT_CHROME_DIR in $EnvFile (a folder for the agent Chrome; see .env.example)."
  exit 1
}
$userDataDir = [IO.Path]::GetFullPath($userDataDir).TrimEnd('\')
$browserProcess = Get-AgentBrowserProcess $userDataDir

if ($Command -eq 'status') {
  if ($browserProcess) { Write-Host "The agent Chrome runs (process $($browserProcess.ProcessId))."; exit 0 }
  Write-Host 'The agent Chrome does not run. Start it with: pwsh scripts/agent-chrome.ps1 start'
  exit 1
}

if ($Command -eq 'restore') {
  if (-not $browserProcess) {
    Write-Host 'The agent Chrome does not run. Start it with: pwsh scripts/agent-chrome.ps1 start'
    exit 1
  }
  Add-Type @'
using System; using System.Runtime.InteropServices;
public static class AgentChromeWindows {
  public delegate bool EnumProc(IntPtr handle, IntPtr parameter);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr parameter);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr handle);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr handle);
  [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr handle, int command);
}
'@
  $restored = [Collections.Generic.List[IntPtr]]::new()
  [void][AgentChromeWindows]::EnumWindows({
      param($handle, $parameter)
      $processId = [uint32]0
      [void][AgentChromeWindows]::GetWindowThreadProcessId($handle, [ref]$processId)
      if ($processId -eq $browserProcess.ProcessId -and [AgentChromeWindows]::IsWindowVisible($handle) -and
        [AgentChromeWindows]::IsIconic($handle)) {
        [void][AgentChromeWindows]::ShowWindowAsync($handle, $ShowNoActivate)
        $restored.Add($handle)
      }
      $true
    }, [IntPtr]::Zero)
  Write-Host "Restored $($restored.Count) minimized window(s) of the agent Chrome."
  exit 0
}

if ($browserProcess) {
  Write-Host "The agent Chrome already runs (process $($browserProcess.ProcessId)). Run restore when its window is minimized."
  exit 0
}

$sourceName = Get-EnvValue $EnvFile 'HEALTHYFAC_AGENT_CHROME_SOURCE_PROFILE'
$profileName = if ($sourceName) { $sourceName } else { 'Default' }
$targetProfile = Join-Path $userDataDir $profileName
if ($sourceName -and -not (Test-Path -LiteralPath $targetProfile)) {
  $defaultUserData = Join-Path $env:LOCALAPPDATA 'Google\Chrome\User Data'
  $sourceProfile = Join-Path $defaultUserData $sourceName
  if (-not (Test-Path -LiteralPath $sourceProfile)) { Write-Host "No Chrome profile folder $sourceProfile."; exit 1 }
  New-Item -ItemType Directory -Force -Path $userDataDir | Out-Null
  # Local State holds the key of the saved sign-ins; without it the copy loses them.
  Copy-Item -Force (Join-Path $defaultUserData 'Local State') (Join-Path $userDataDir 'Local State')
  robocopy $sourceProfile $targetProfile /MIR /R:0 /W:0 /NFL /NDL /NJH /NJS /NP `
    /XD Cache 'Code Cache' GPUCache DawnGraphiteCache DawnWebGPUCache CacheStorage | Out-Null
  if ($LASTEXITCODE -ge 8) { Write-Host "The profile copy failed (robocopy exit $LASTEXITCODE)."; exit 1 }
}

Start-Process -FilePath $ChromePath -ArgumentList @(
  "--user-data-dir=`"$userDataDir`""
  "--profile-directory=`"$profileName`""
  '--force-device-scale-factor=1'
  '--disable-renderer-backgrounding'
  '--disable-backgrounding-occluded-windows'
  '--disable-features=CalculateNativeWinOcclusion'
  '--window-size=1920,1080'
  '--window-position=0,0'
  '--no-first-run'
)
Write-Host "Started the agent Chrome ($userDataDir). In a new session, choose it with /chrome."
exit 0
