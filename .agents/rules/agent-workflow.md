---
trigger: always_on
description: "Rules for the scope of agent work, changes outside a task, and the final report."
---
# Agent Workflow Rules
These rules apply to every agent and to the main session.
* **Scope:** Change only what the task asks for. When you see a needed change outside the task, report it and do not make it, unless the task cannot pass without it. A small necessary change (a missing helper, an import, a file move) may proceed and must be logged. A change to behavior, an acceptance criterion, or the design must not: stop and report it.
* **Detour Report:** List every change outside the plan in your report, with its reason and its files. When a plan.md exists, also log it under `## Deviations` in plan.md, so the reviewer sees it. This includes refactors, renames, moved or merged types, changed queries, changed configuration, and changed or removed tests.
* **After a Review:** When a review has checked the work, change only what the review fixes ask for. Report any other change that you think is needed, and do not make it.
* **Proven Removal:** Never remove or weaken code that a comment, a document, a rule, or the plan explains (e.g., a transaction wrapper, a retry, a guard, a filter, a lock, a timeout). Passing tests do not prove that such code is unnecessary: another mechanism can hide its absence (e.g., a framework retry). If you think the code is not needed, keep it, write a test that fails without it, and report the result. The user decides.
* **Self-Check:** Before you report, check your changes against the rule files that match their paths. List only the rules that you could not follow, with the reason. Do not list the rules that you followed.
* **Rule Enforcement:** When you propose a rule, also propose a deterministic check that enforces it: code (e.g., an analyzer, a type, an `.editorconfig` severity), a test (e.g., an architecture test), or a shell check (e.g., a hook, a script). When no check can enforce the rule, say so and give the reason.