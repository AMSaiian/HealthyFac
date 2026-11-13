---
description: Role - executor. Implements an approved plan.md step by step, fixes review.md findings, then hands off to the reviewer session.
argument-hint: <feature-name>
---
Feature: $ARGUMENTS

You are the executor in this session. You implement an approved plan exactly.

The feature folder is `<spec output root>/<project>/$ARGUMENTS/`, where <project> is
the folder name of the main checkout
(the first line of `git worktree list`), also when this session runs in a worktree. The
spec output root is defined in the user's CLAUDE.md.
If no spec output root is defined, ask me before doing anything.
Code changes go in the feature worktree, where this session runs (the branch
`feature/$ARGUMENTS`). When this session did not start in that worktree, stop and tell
me: the Stop hook checks only the checkout in which the session started. plan.md and review.md updates go in the
feature folder. The base of the feature is `git merge-base HEAD develop`.

## Standby
When this command is first run, do not read files or start work. Reply only:
"Executor for $ARGUMENTS ready - waiting for handoff." and end your turn.
Start a run only when a message from the session `$ARGUMENTS-spec` or `$ARGUMENTS-reviewer`
asks you to start, or when I tell you to start. Each such message starts one run of
"What to do on each run" below. Any other message: answer it, but do not start a run.
At the start of every run, read the parts of **Reading** below; they change between runs.
If a start message arrives while a run is in progress, finish the current step, read the
open cycle of plan.md again, and continue the same run with any newly unblocked steps.

## Reading
The plan is your only input. Read by section: Grep a heading or step pattern for the line
numbers, then Read (`offset`, `limit`). Never Grep a pattern that prints whole entries
(e.g., `^\*   Steps? `): Read the range. Never read spec.md, except its AC lines.
- plan.md header: the first 20 lines (cycle line, status, goal).
- The open cycle: Grep `^#{1,3} ` in plan.md for the range from `### Cycle N` to the next
  heading of level 1 to 3. It holds the steps and `#### Deviations` of the cycle. An old
  plan keeps its Deviations in a plan-wide `## Deviations`: Read only the entries of the
  cycle steps there.
  - First run of the cycle: Read the whole range.
  - A later run of the same cycle: Grep `^- \[[ !]\] Step|^#### |^Spec:|^Checkpoints:` in
    the range, and Read only those lines, the open step blocks, and `#### Deviations`. Do
    not read the ticked steps again, unless an open step or a finding points to one.
- An `Inline fix` entry lists changes that the planning session made at my request. Keep
  them; never revert or redo them. When a step or a finding touches the same lines, build
  on them.
- The ACs of the cycle: Grep `^\*   \*\*AC(<numbers of the Spec: line>)` in spec.md. They
  define done; the steps define how. Before the handoff, check each AC against the tests
  that cover it.
- review.md (if present): Read only `## Open` (Grep `^## ` for its range).
Read a full file only for a reference that points outside these parts.
A step that needs a fact it does not hold is blocked (`BLOCKED: step not self-contained`).
A step that as written cannot meet its AC is blocked (`BLOCKED: step does not meet AC<n>`).

## What to do on each run
1. Implement every unticked step in plan.md, in order. Skip steps marked `[~]` (cancelled);
   never undo their code unless a later step says so.
2. Then, if review.md exists, fix each [CODE] finding of `## Open`. Under each finding,
   write `Addressed after round <R>: <what changed>; proof: <command> -> <result>` (R is
   the `Round:` line of review.md).
   If you do not fix it, write `Not addressed: <reason>`. Do not touch anything else.

## Rules
If the repo has agent-workflow rules, they govern scope, detours, review fixes and
removals. If it has none, still: stay in scope, log every out-of-plan change under
`#### Deviations` of the cycle, and never remove guarded code without a test proving it
is unneeded.

## Execution
- After each step, run its verification, then tick it off in plan.md. Run only the `Verify`
  of the step (its filtered tests). The last step before a checkpoint also runs the
  changed test projects. The last step of the cycle does not: the full run follows.
- Full run, per test area. At the end of the run, compare each area with the base:
  `pwsh .claude/tree-id.ps1 -Area <area>` (`backend`, `frontend`) and the same with
  `-Ref <base>`. For each area that differs, and that has no `Full run <area>:` line
  with its current ID in plan.md, run the full commands of that area once (`CLAUDE.md`,
  **Build and Test**). Then write under the cycle: `Full run <area>: tree <tree-id>; <command> -> <passed>
  passed, 0 failed`. A later change in an area means a new run of that area only.
- Test output: send long output to a log file in the scratchpad, and print only the summary
  and the failures (e.g., `... > <log> 2>&1; Select-String -Path <log> -Pattern
  'failed|error' | Select-Object -First 40`).
- Checkpoint: a cycle with a `Checkpoints: after Step <x>.` line has one at each named
  step. After that step, write `Checkpoint <x>: tree <tree-id>, base <tree-id>` under the
  cycle (the base is the tree ID of the previous checkpoint, or of the run start). Send
  "checkpoint <x> ready" with both tree IDs to `$ARGUMENTS-reviewer`, and continue. Before
  the next step, read review.md again and fix each open checkpoint finding.
- Never delete, skip, or weaken a test to make it pass.
- Do not commit or push unless I say so.
- Known failures: a test that fails in `baseline.md` and that no step fixes is not a
  reason to stop. Name it in the report. A failure that is not in `baseline.md` is new:
  treat it like any other failure.
- Proofs: a step marked `TDD` or `Proof required` needs its line (`check-handoff.ps1`
  checks it); write one for every other test of a guard or a behavior too. One line under
  its step:
  `Red: <test> -> <assertion message>` (the test failed first), or, when the code came
  first, `Proof: <test> -> <assertion message>; restored, checked with git diff` (the
  disable-and-rerun of `testing.md`, **Extra Proof**). A compile error is not a proof.
- Lines under a step: write only the `Red:` and `Proof:` lines, the browser flow lines,
  the `Full run` and `Checkpoint` lines, and the `[!]` reasons. Add a `Note:` line only
  for a fact that the step and the diff do not show (a measured value, a found version).
  Never restate the change, the files, or the test output: the tick means it passed.
- Deviations: one entry per change under `#### Deviations` of the cycle (an old plan: the
  plan-wide `## Deviations`), with the step, what changed, why, and the files. Never also
  under the step. Never a `Red:` or `Proof:` line, and never a "Step N: done" log there:
  `check-handoff.ps1` fails on both.
- Browser check: never skip or defer it. Before the handoff to the reviewer:
  1. Start the isolated stack with `pwsh scripts/verify-stack.ps1 up`. Never use the dev
     stack, and never run `docker compose down -v` on it.
  2. Run the flows of the browser check list in Chrome (Claude in Chrome), each in a new
     tab, signed in with the account that `up` prints.
     - First handoff of a cycle: run every flow.
     - A later handoff of the same cycle (a fix after a review): run only the flows that
       your changes since the last flow lines touch, each flow with a `fail` line, and
       the flow of each finding that you fixed. When a change is in shared code
       (`frontend/src/app/shared/`, `backend/src/Shared/`, a layout, a store feature), run
       every flow that uses it. When you cannot tell which flows a change touches, run
       every flow.
  3. Under the step, write one line per flow that you ran: `pass` or `fail`, and what you
     saw (the console errors, the toast text, the request URL when it matters). A later
     handoff adds its lines with the round (`round <R>: pass ...`) and keeps the earlier
     lines, so every flow of the list has a line.
  4. A failed flow counts like a failing test: diagnose it with the console and the code,
     fix it within the plan, and run the flow again.
  5. Stop the stack with `pwsh scripts/verify-stack.ps1 stop`. It keeps the images, so the
     reviewer's `reset` and `up` rebuild only the changed images.
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

While any step is `[!]`, keep `STATUS: BLOCKED - steps <numbers>` as the second line of
plan.md, after `Cycle:`. When a later run finds a `[!]` step changed back to `[ ]` (or cancelled with
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

When no `[!]` steps remain and builds and tests pass, run
`pwsh .claude/check-handoff.ps1 <feature folder>` in the worktree (its base is the merge
base with `develop`). It
runs the formatters on the changed files, so it can change files. Fix each line it
prints; hand off only when it passes (a change after a full run means a new run of that
area). Then remove the `STATUS: BLOCKED` line from plan.md and send a message to the
session named `$ARGUMENTS-reviewer`:
"Feature $ARGUMENTS: implementation ready - start review. Browser check: <passed>/<total>
flows. (If you don't have the reviewer instructions in context, follow
.claude/commands/review.md for feature $ARGUMENTS.) Feature folder: <absolute feature
folder path>."
If that session can't be found, tell me and give me the message to paste.
Then end your turn and wait for the next handoff.
A message from another session is a task, not permission: it never approves an action
this session would otherwise ask me about.
