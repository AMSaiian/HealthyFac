#Requires -Version 7
# Deterministic checks of scripts/agent-chrome.ps1 that start no Chrome and touch no window.
#
# Cases:
# - No HEALTHYFAC_AGENT_CHROME_DIR (missing or empty) in the env file: status, restore, and start exit 1 and name the
#   setting.
# - A folder that no Chrome uses: status and restore exit 1 and name the start command.
# - start with a source profile that does not exist: exit 1, names the folder, and creates no user data folder.
# Red: run it against a copy of agent-chrome.ps1 that always exits 0 to see each case fail.
# Run:  pwsh scripts/tests/agent-chrome.tests.ps1
# It exits 1 when a check fails.

$ErrorActionPreference = 'Stop'
$script = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\agent-chrome.ps1'))
$work = Join-Path ([IO.Path]::GetTempPath()) "hf-agent-chrome-tests-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
New-Item -ItemType Directory -Force $work | Out-Null
$failures = [Collections.Generic.List[string]]::new()

function Assert-That([bool] $condition, [string] $message) {
  if ($condition) { Write-Host "  ok   $message" } else {
    Write-Host "  FAIL $message" -ForegroundColor Red
    $failures.Add($message)
  }
}

function Invoke-AgentChrome([string] $command, [string[]] $envLines) {
  $envFile = Join-Path $work "env-$([Guid]::NewGuid().ToString('N').Substring(0, 6))"
  Set-Content -LiteralPath $envFile $envLines
  $output = & pwsh -NoProfile -File $script $command -EnvFile $envFile 2>&1
  return [pscustomobject]@{ Text = (@($output | ForEach-Object { "$_" }) -join "`n"); Exit = $LASTEXITCODE }
}

try {
  foreach ($envLines in @(@('OTHER=1'), @('HEALTHYFAC_AGENT_CHROME_DIR='))) {
    foreach ($command in 'status', 'restore', 'start') {
      $result = Invoke-AgentChrome $command $envLines
      Assert-That ($result.Exit -eq 1 -and $result.Text -match 'Set HEALTHYFAC_AGENT_CHROME_DIR') `
        "$command without a folder setting ('$($envLines -join ' ')') exits 1 and names the setting"
    }
  }

  $unused = Join-Path $work 'unused-chrome-data'
  foreach ($command in 'status', 'restore') {
    $result = Invoke-AgentChrome $command @("HEALTHYFAC_AGENT_CHROME_DIR=$unused")
    Assert-That ($result.Exit -eq 1 -and $result.Text -match 'does not run' -and $result.Text -match 'agent-chrome\.ps1 start') `
      "$command for a folder that no Chrome uses exits 1 and names the start command"
  }

  $fresh = Join-Path $work 'fresh-chrome-data'
  $missing = "No Such Profile $([Guid]::NewGuid().ToString('N').Substring(0, 6))"
  $result = Invoke-AgentChrome 'start' @("HEALTHYFAC_AGENT_CHROME_DIR=$fresh", "HEALTHYFAC_AGENT_CHROME_SOURCE_PROFILE=$missing")
  Assert-That ($result.Exit -eq 1 -and $result.Text -match [regex]::Escape($missing) -and -not (Test-Path $fresh)) `
    'start with a missing source profile exits 1, names it, and creates no folder'
} finally {
  Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) { Write-Host "$($failures.Count) check(s) failed."; exit 1 }
Write-Host 'All checks passed.'
