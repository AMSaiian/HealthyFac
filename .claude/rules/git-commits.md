# Git Commit Rules
*   **Subject Format:** Write each commit subject as `<type>(<category>): <message>`.
    *   **Type:** Use `feature` for new or changed behavior. Use `bug` for a defect fix.
    *   **Category:** Use `setup` for project setup, tooling, tests infrastructure, and agent rules. Use `docs` for documentation. Use the feature name for feature work (e.g., `messaging`, `machines`).
    *   **Message:** Write a short summary in lower case (e.g., `feature(messaging): added outbox and inbox`).
*   **One Category per Commit:** Split changes of different categories into separate commits.
*   **Authorship:** Commit with the git identity of the user. NEVER commit as Claude or as another agent. Never add `Co-Authored-By`, `Claude-Session`, or another agent attribution line to a commit message or a pull request description.
*   **History:** Keep the history of a branch clean.
    *   One commit per logical change. A cycle is one commit group (tooling, docs, code), committed after the reviewer approves it.
    *   A change of code from an earlier commit of the branch is a fixup. The plan step has `Fixup: <subject>`, and `commit-cycle.ps1` commits it with `git commit --fixup=<SHA>`. Never add a standalone commit for it. A cycle of only fixup steps has no Commit line.
    *   Before each handoff, `commit-cycle.ps1` runs `git rebase --autosquash <squash base>`. The HEAD tree stays the same, and no `fixup!` commit stays above the squash base. Only HEAD is tested.
    *   The squash base is the `Reviewed by Artur:` SHA of plan.md, else its `Base:` SHA, else `git merge-base HEAD develop`.
    *   After the last cycle, a new reviewer reviews `git diff <base>...HEAD` (`review branch`). Its fixes are `Fixup:` steps.
    *   History that Artur reviewed is never rewritten. Fixes for his comments are fixup commits on top, one per comment or group. They fold only after his "review finished" (`commit-cycle.ps1 -Squash`).
    *   Agents never push and never amend. Agents rebase only through `commit-cycle.ps1`.
