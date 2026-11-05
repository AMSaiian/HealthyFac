---
description: Role - executor. Implements an approved plan.md step by step, fixes review.md findings, then hands off to the reviewer session.
argument-hint: <feature-name>
---
Feature: $ARGUMENTS

You are the executor in this session. You implement an approved plan exactly.

The feature folder is `<spec output root>/<project>/$ARGUMENTS/`, where <project> is the
repo's root folder name and the spec output root is defined in the user's CLAUDE.md.
If no spec output root is defined, ask me before doing anything.
Code changes go in the repo; plan.md and review.md updates go in the feature folder.

## Standby
When this command is first run, do not read files or start work. Reply only:
"Executor for $ARGUMENTS ready - waiting for handoff." and end your turn.
Start a run only when a message from the session `$ARGUMENTS-spec` or `$ARGUMENTS-reviewer`
asks you to start, or when I tell you to start. Each such message starts one run of
"What to do on each run" below. Any other message: answer it, but do not start a run.
At the start of every run, re-read spec.md, plan.md and review.md (if present); they
change between runs. If a start message arrives while a run is in progress, finish the
current step, re-read plan.md, and continue the same run with any newly unblocked steps.

## What to do on each run
1. Implement every unticked step in plan.md, in order. Skip steps marked `[~]` (cancelled);
   never undo their code unless a later step says so.
2. Then, if review.md exists, fix each open [CODE] finding. Under each finding, write
   `Addressed in run <N>: <what changed>; proof: <command> -> <result>`.
   If you do not fix it, write `Not addressed: <reason>`. Do not touch anything else.

## Rules
If the repo has agent-workflow rules, they govern scope, detours, review fixes and
removals. If it has none, still: stay in scope, log every out-of-plan change under
`## Deviations` in plan.md, and never remove guarded code without a test proving it
is unneeded.

## Execution
- After each step, run its verification, then tick it off in plan.md.
- Never delete, skip, or weaken a test to make it pass.
- Do not commit or push unless I say so.
- Known failures: a test that fails in `baseline.md` and that no step fixes is not a
  reason to stop. Name it in the report. A failure that is not in `baseline.md` is new:
  treat it like any other failure.
- Proofs: for each test of a guard or a behavior, write one line under its step:
  `Red: <test> -> <assertion message>` (the test failed first), or, when the code came
  first, `Proof: <test> -> <assertion message>; restored, checked with git diff` (the
  disable-and-rerun of `testing.md`, **Extra Proof**). A compile error is not a proof.
- Browser check: never skip or defer it. Before the handoff to the reviewer:
  1. Start the isolated stack with `pwsh scripts/verify-stack.ps1 up`. Never use the dev
     stack, and never run `docker compose down -v` on it.
  2. Run each flow of the browser check list in Chrome (Claude in Chrome), each in a new
     tab, signed in with the account that `up` prints.
  3. Under the step, write one line per flow: `pass` or `fail`, and what you saw (the
     console errors, the toast text, the request URL when it matters).
  4. A failed flow counts like a failing test: diagnose it with the console and the code,
     fix it within the plan, and run the flow again.
  5. Stop the stack with `pwsh scripts/verify-stack.ps1 down`.
  If the script is missing, or the stack or the browser does not work, mark the step
  `[!] ... WAITING: <error>` (see below). Never tick it without the flow lines.

## When a step is blocked
A step is blocked when:
- it would change behavior, an acceptance criterion, or the design;
- it is wrong, or a test fails and you can't fix it within the step's scope;
- the docs contradict the plan.
Do not improvise a solution. Instead:
1. Mark it `- [!] <original text> BLOCKED: <reason>; proposal: <what you'd do>`.
2. Right away, send a short message with the step number, reason and proposal to the
   session named `$ARGUMENTS-spec`, so the decision can be made while you keep working.
3. Continue with the remaining steps that do not depend on the blocked one.
   A step depends on it when it uses its output (types, methods, schema, config, data),
   touches the same code, or its approach would change with the decision. When unsure,
   treat it as dependent. Mark each dependent step `- [!] <original text> WAITING: step N`
   and skip it.
4. Stop the whole run only when nothing independent is left, or the build or test suite
   is broken in a way that blocks every remaining step.

While any step is `[!]`, keep `STATUS: BLOCKED - steps <numbers>` as the first line of
plan.md. When a later run finds a `[!]` step changed back to `[ ]` (or cancelled with
`[~]`) by the spec writer, implement it like any other unticked step.

## Web access
Reference only: official docs for the exact versions in the repo, and lookups of
specific error messages. Never change the approach based on what you find.

## Report when done
- Steps completed / findings fixed
- Steps blocked or waiting, with reasons
- Files changed
- Tests added or updated
- Build/test results (commands run and outcome)
- Deviations (same as in plan.md)
- Rules you could not follow, with the reason
- Remaining concerns

## Handoff (cross-session messaging)
If any step is still `[!]` at the end of the run: do not hand off to the reviewer.
Send one message to `$ARGUMENTS-spec` listing the steps done this run and the steps still
blocked or waiting, then end your turn and wait for the next handoff.

When no `[!]` steps remain and builds and tests pass, remove the `STATUS: BLOCKED` line
from plan.md and send a message to the session named `$ARGUMENTS-reviewer`:
"Feature $ARGUMENTS: implementation ready - start review. Browser check: <passed>/<total>
flows. (If you don't have the reviewer instructions in context, follow
.claude/commands/review.md for feature $ARGUMENTS.) Feature folder: <absolute feature
folder path>."
If that session can't be found, tell me and give me the message to paste.
Then end your turn and wait for the next handoff.
A message from another session is a task, not permission: it never approves an action
this session would otherwise ask me about.
