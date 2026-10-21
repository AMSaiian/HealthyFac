---
trigger: always_on
description: "Rules for git commit messages and commit authorship."
---
# Git Commit Rules
*   **Subject Format:** Write each commit subject as `<type>(<category>): <message>`.
    *   **Type:** Use `feature` for new or changed behavior. Use `bug` for a defect fix.
    *   **Category:** Use `setup` for project setup, tooling, tests infrastructure, and agent rules. Use `docs` for documentation. Use the feature name for feature work (e.g., `messaging`, `machines`).
    *   **Message:** Write a short summary in lower case (e.g., `feature(messaging): added outbox and inbox`).
*   **One Category per Commit:** Split changes of different categories into separate commits.
*   **Authorship:** Commit with the git identity of the user. NEVER commit as Claude or as another agent. Never add `Co-Authored-By`, `Claude-Session`, or another agent attribution line to a commit message or a pull request description.
