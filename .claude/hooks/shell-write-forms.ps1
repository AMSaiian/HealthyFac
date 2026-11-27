#Requires -Version 7
# shell-write-forms.ps1 - dot-sourced by reviewer-shell-scope.ps1 and handoff-write-guard.ps1: the one list of the shell
# write forms that name no file (the hook that uses it adds its own file-bound forms): sed -i, perl -i, Set-Content,
# Add-Content, Out-File, writeFileSync, appendFileSync, write_text, .write(, WriteAllText, and WriteAllLines.
$script:SharedWriteForms = @(
  '(?i)\b(sed|perl)\b[^|;&]*\s-i',
  '(?i)\b(Set-Content|Add-Content|Out-File)\b',
  '(?i)\b(writeFileSync|appendFileSync|write_text|WriteAllText|WriteAllLines)\b',
  '(?i)\.write\('
)
