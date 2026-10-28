---
trigger: always_on
description: "Rules for the scope of agent work, changes outside a task, and the final report."
---
# Agent Workflow Rules
*   **Scope:** Change only what the task asks for. When you see a needed change outside the task, report it and do not make it, unless the task cannot pass without it.
*   **Detour Report:** List every change outside the plan in the report, with its reason and its files. This includes refactors, renames, moved or merged types, changed queries, changed configuration, and changed or removed tests.
*   **After a Review:** When a review has checked the work, change only what the review fixes ask for. Report any other change that you think is needed, and do not make it.
*   **Proven Removal:** Never remove or weaken code that a comment, a document, a rule, or the plan explains (e.g., a transaction wrapper, a retry, a guard, a filter, a lock, a timeout). Passing tests do not prove that such code is unnecessary: another mechanism can hide its absence (e.g., a framework retry). If you think the code is not needed, keep it, write a test that fails without it, and report the result. The user decides.
*   **Self-Check:** Before you report, compare each changed file with every rule file that matches its path. List each rule that you could not follow, and the reason.
