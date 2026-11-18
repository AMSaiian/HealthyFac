# Dot-sourced by verify-changed.ps1, cleanup-changed.ps1, and format-changed.ps1.
# Windows limits the command line of a process: 32767 characters for a program, 8191 for a .cmd or .bat file (cmd.exe).
# A longer line does not start. PowerShell 7.7 then reports "StandardOutputEncoding is only supported when standard
# output is redirected" instead of the real cause. A checkout with several hundred changed files passes that limit, so
# each call that takes the file list on its command line runs once per batch of Split-ArgumentBatches.

# The characters that the file arguments of one call may use: the limit of the command minus room for the path of the
# program and its other arguments.
function Get-ArgumentBudget([string] $command) {
  $limit = if ([IO.Path]::GetExtension($command) -in '.cmd', '.bat') { 8191 } else { 32767 }
  $limit - 2048
}

# Splits the items into batches whose length, with a separator and two quotes for each item, stays within $budget.
# Each batch is one array in the output. An item longer than the budget gets a batch of its own.
function Split-ArgumentBatches([string[]] $items, [int] $budget) {
  $batch = [Collections.Generic.List[string]]::new()
  $length = 0
  foreach ($item in $items) {
    $cost = $item.Length + 3
    if ($batch.Count -gt 0 -and $length + $cost -gt $budget) {
      , $batch.ToArray()
      $batch.Clear()
      $length = 0
    }
    $batch.Add($item)
    $length += $cost
  }
  if ($batch.Count -gt 0) { , $batch.ToArray() }
}
