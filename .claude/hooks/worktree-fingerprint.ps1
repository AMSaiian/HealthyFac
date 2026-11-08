# Dot-sourced by record-turn-start.ps1 and verify-changed.ps1. Git only.

# A fingerprint of the working tree of a checkout: the output of "git status --porcelain=v1 -z -uall" plus the git
# content hash of every file that the status lists (changed, added, and untracked). It changes when a file is added,
# removed, or edited, and stays the same otherwise. A deleted file shows in the status entry only. The -z form keeps
# every path raw (no C quoting of non-ASCII characters), and the UTF-8 encodings keep those paths intact on the way
# through git. A rename or copy entry holds the old path in the next field, which the loop skips.
function Get-WorktreeFingerprint([string] $repo) {
  $previousConsole = [Console]::OutputEncoding
  $previousPipe = $OutputEncoding
  $utf8 = [Text.UTF8Encoding]::new($false)
  [Console]::OutputEncoding = $utf8
  $OutputEncoding = $utf8
  try {
    $fields = ((@(git -C $repo status --porcelain=v1 -z -uall 2>$null)) -join '').Split(
      [char]0, [StringSplitOptions]::RemoveEmptyEntries)
    $entries = [Collections.Generic.List[string]]::new()
    $paths = [Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $fields.Count; $index++) {
      $field = $fields[$index]
      $entries.Add($field)
      $paths.Add($field.Substring(3))
      if ($field[0] -in 'R', 'C' -or $field[1] -in 'R', 'C') {
        $index++
        if ($index -lt $fields.Count) { $entries.Add($fields[$index]) }
      }
    }
    $existing = @($paths | Where-Object { Test-Path -LiteralPath (Join-Path $repo $_) -PathType Leaf })
    $hashes = if ($existing.Count -gt 0) { @($existing | git -C $repo hash-object --stdin-paths 2>$null) } else { @() }
    $text = ((@($entries) + $hashes) -join "`n")
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
  }
  finally {
    [Console]::OutputEncoding = $previousConsole
    $OutputEncoding = $previousPipe
  }
}

# The absolute path of a state file in the git directory of the checkout (relative in the main checkout, absolute in a
# worktree).
function Get-GitStatePath([string] $repo, [string] $name) {
  [IO.Path]::GetFullPath((git -C $repo rev-parse --git-path $name), $repo)
}

# The file that holds the fingerprint of a session at the start of its turn.
function Get-TurnFingerprintPath([string] $repo, [string] $sessionId) {
  $safe = $sessionId -replace '[^A-Za-z0-9_-]', '_'
  Get-GitStatePath $repo "claude-turn-$safe.txt"
}

# Deletes the turn fingerprint files that no session has written for longer than $maxAge. Each prompt of a live session
# rewrites its file, so only the files of ended sessions age out.
function Remove-StaleTurnFingerprints([string] $repo, [TimeSpan] $maxAge) {
  $directory = Split-Path (Get-GitStatePath $repo 'claude-turn-x.txt')
  if (-not (Test-Path -LiteralPath $directory -PathType Container)) { return }
  $limit = [DateTime]::UtcNow - $maxAge
  Get-ChildItem -LiteralPath $directory -Filter 'claude-turn-*.txt' -File |
    Where-Object { $_.LastWriteTimeUtc -lt $limit } |
    Remove-Item -Force -ErrorAction SilentlyContinue
}
