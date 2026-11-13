#Requires -Version 7
# Deterministic check of the rule files (CLAUDE.md, Rules). Claude Code reads only the `paths` frontmatter field of a
# rule, loads a rule without `paths` at launch, and loads an `@` import at launch even from a scoped rule. So:
# - a rule file has no frontmatter, or a frontmatter with the one key `paths` and a list of quoted patterns;
# - no rule file imports another file with `@`;
# - CLAUDE.md imports no file with `@` (each import loads in every session);
# - the rule table of CLAUDE.md has one row per rule file, with the same paths ("always loaded" for no paths).
# Run:  pwsh .claude/tests/rules.tests.ps1 [-Repo <path>]
# It exits 1 when a check fails.
param([string]$Repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))

$ErrorActionPreference = 'Stop'
$failures = [Collections.Generic.List[string]]::new()
function Assert-That([bool] $condition, [string] $message) {
  if ($condition) { Write-Host "  ok   $message" } else {
    Write-Host "  FAIL $message" -ForegroundColor Red
    $failures.Add($message)
  }
}

$rules = @{}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Repo '.claude/rules') -Filter '*.md' -Recurse) {
  $lines = @(Get-Content -LiteralPath $file.FullName)
  $name = $file.BaseName
  $paths = @()
  $bodyStart = 0
  $isFrontmatterValid = $true
  if ($lines.Count -gt 0 -and $lines[0] -eq '---') {
    $close = [Array]::IndexOf($lines, '---', 1)
    if ($close -lt 2 -or $lines[1] -ne 'paths:') { $isFrontmatterValid = $false } else {
      foreach ($line in $lines[2..($close - 1)]) {
        $item = [regex]::Match($line, '^  - "([^"]+)"\s*$')
        if ($item.Success) { $paths += $item.Groups[1].Value } else { $isFrontmatterValid = $false }
      }
      $bodyStart = $close + 1
    }
    if ($paths.Count -eq 0) { $isFrontmatterValid = $false }
  }
  Assert-That $isFrontmatterValid "$name`: the frontmatter is absent, or holds only 'paths:' with a list of quoted patterns"
  $imports = @($lines | Select-Object -Skip $bodyStart | Where-Object { $_ -match '^\s*@\S' })
  Assert-That ($imports.Count -eq 0) "$name`: no @ import"
  $rules[$name] = $paths
}

$claude = @(Get-Content -LiteralPath (Join-Path $Repo 'CLAUDE.md'))
Assert-That (@($claude | Where-Object { $_ -match '^\s*@\S' }).Count -eq 0) 'CLAUDE.md imports no file with @'

$rows = @{}
foreach ($line in $claude) {
  $row = [regex]::Match($line, '^\|\s*`([a-z0-9-]+)`\s*\|\s*(.+?)\s*\|\s*$')
  if (-not $row.Success) { continue }
  $cell = $row.Groups[2].Value
  $rows[$row.Groups[1].Value] = if ($cell -eq 'always loaded') { @() } else {
    @([regex]::Matches($cell, '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
  }
}
$missingRows = @($rules.Keys | Where-Object { -not $rows.ContainsKey($_) } | Sort-Object)
$extraRows = @($rows.Keys | Where-Object { -not $rules.ContainsKey($_) } | Sort-Object)
Assert-That ($missingRows.Count -eq 0) "every rule file has a row in the table of CLAUDE.md (missing: $($missingRows -join ', '))"
Assert-That ($extraRows.Count -eq 0) "every row of the table names a rule file (extra: $($extraRows -join ', '))"
foreach ($name in $rules.Keys | Where-Object { $rows.ContainsKey($_) } | Sort-Object) {
  Assert-That ((@($rules[$name]) -join '|') -eq (@($rows[$name]) -join '|')) "$name`: the table row equals the paths of the file"
}

if ($failures.Count -gt 0) {
  Write-Host "`n$($failures.Count) check(s) failed." -ForegroundColor Red
  exit 1
}
Write-Host "`nAll checks passed."
