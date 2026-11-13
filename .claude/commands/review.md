---
description: Role - reviewer. Reviews the implementation against spec.md, plan.md and the workflow rules; writes review.md and routes findings to the executor or spec session. No code changes.
argument-hint: <feature-name>
---
Feature: $ARGUMENTS

You are the reviewer in this session. Do not edit, create or delete any file in the repo.
The only files you write are review.md and the screenshots under `screens/` in the
feature folder.

The feature folder is `<spec output root>/<project>/$ARGUMENTS/`, where <project> is
the folder name of the main checkout
(the first line of `git worktree list`), also when this session runs in a worktree. The
spec output root is defined in the user's CLAUDE.md.
If no spec output root is defined, ask me before doing anything.

## Standby
When this command is first run, do not read files or start work. Reply only:
"Reviewer for $ARGUMENTS ready - waiting for handoff." and end your turn.
Start a review only when a message from the session `$ARGUMENTS-executor` asks you to,
or when I tell you to. A relay of my words from the session `$ARGUMENTS-spec` counts as
my request. For a review of inline fixes, review only the files that the relay lists and
their `Inline fix` Deviations entry, in the open round. Any other message: answer it, but
do not start a review.
At the start of every review, read the parts of **Reading** below, then the diff (**Rounds**).

## Reading
Read by section: Grep a heading pattern for the line numbers, then Read (`offset`, `limit`).
Never Grep a pattern that prints whole entries (e.g., `^\*   Steps? `): Read the range.
- plan.md header: the first 20 lines.
- The open cycle: Grep `^#{1,3} ` in plan.md, take the range from `### Cycle N` to the next
  heading of level 1 to 3, and Read it. It holds the steps, the `Red:` and `Proof:` lines,
  the `Full run` lines, and `#### Deviations` (with the `Inline fix, cycle N` entries: changes
  of the planning session at my request). An old plan keeps its Deviations in a plan-wide
  `## Deviations`: Read only the entries of the cycle steps there.
- The ACs of the cycle: Grep `^\*   \*\*AC(<numbers of the Spec: line>)` in spec.md, and the
  rows of the cycle steps under `## Acceptance Criteria Map` in plan.md.
- The spec sections of the `Spec:` line of the cycle: Grep each heading in spec.md, and
  Read up to the next heading of the same level.
- review.md (if present): read it whole. It holds only the open findings and one line per
  closed finding and per round.
Read a full file only for a reference that points outside these parts.

## Rounds
- Round 1 of a cycle: review the whole cycle: `git diff <base>` in the feature worktree
  (the base is `git merge-base HEAD develop`), and the checks below.
- A later round of the same cycle: review `git diff <Reviewed tree>` (the tree of the last
  round, in review.md) and each open finding. Check again only the parts that this diff
  touches. When the diff changes shared code (`backend/src/Shared/`,
  `frontend/src/app/shared/`, a fixture, a migration), review the callers of that code too.
- At the end of each round, write `Reviewed tree: <id>` (`pwsh .claude/tree-id.ps1 -Repo
  <repo path>`) as the third line of review.md.

## Check
- Every acceptance criterion is met, with a test where possible.
- No scope creep: every change outside plan.md appears in the Deviations of the cycle with
  a reason.
- No guarded code (transactions, retries, guards, locks, filters, timeouts) was removed
  or weakened without a test that fails without it.
- No test was deleted, skipped or weakened.
- Cancelled steps (`[~]`): their code is changed or reverted as the replacing steps require,
  not left half-applied.
- If the repo has agent-workflow rules, the changes follow them.
- Correctness, error handling, concurrency, security, and project conventions.
- Full run, per test area (`backend`, `frontend`): run `pwsh .claude/tree-id.ps1 -Repo
  <repo path> -Area <area>`. When the ID equals a `Full run <area>:` line of plan.md, the
  executor tested this code: do not run that area again, and run only the tests that you
  doubt. Otherwise run the full commands of that area (`implement.md`, **Execution**). An
  old plan with `Full run: tree` lines compares the whole tree (no `-Area`). Use the shell
  only for: build, test, git diff/log, `pwsh scripts/verify-stack.ps1 up|reset`,
  `pwsh .claude/tree-id.ps1`, `pwsh .claude/check-review-routing.ps1`, and the copy of the
  screenshots into the feature folder.
- Checkpoint review: on a message "checkpoint <x> ready" with two tree IDs, review
  `git diff <base tree> <checkpoint tree>` for the design, the rules, and the plan. Run no
  tests. Add the findings to `## Open` with `Status: NEW (checkpoint <x>)`, all [CODE] or
  [SPEC]. Do not change the verdict, and do not run `check-review-routing.ps1`. Send
  [CODE] findings to `$ARGUMENTS-executor` and [SPEC] findings to `$ARGUMENTS-spec`.
- Final review: when the cycle had checkpoints, also review `git diff <last checkpoint
  tree>` for the steps after the last checkpoint. The checks of the whole cycle stay.
- A test that fails in `baseline.md` is known. Report it only when a step should fix it.
- Every test of a guard or a behavior has a `Red:` or `Proof:` line with an assertion
  message under its step. A missing line is a MINOR [CODE] finding; its fix is the proof
  run, not a code change.
- Each new route flow has route flow tests (`frontend-angular.md`, **Tests**). A missing
  one is a MAJOR [CODE] finding.
- For a UI feature in a later round of a cycle: run only the flows that the diff since
  `Reviewed tree:` touches and the flows of the open findings, with their screenshots,
  on the running slot. The next two bullets describe round 1.
- For a UI feature: the browser check list has a `pass` or `fail` line for every flow.
  A missing or partial log is a MAJOR [CODE] finding. Then start a clean isolated stack:
  run `pwsh scripts/verify-stack.ps1 reset`, then `up`, because data that earlier users
  left (moved cards, deleted rows, an old seed) changes the expected results. On the app
  page, run `localStorage.clear(); sessionStorage.clear(); location.reload()` with the
  JavaScript tool: it clears only the app origin, and the sign-in session stays. Then run
  the flows yourself in Chrome (Claude in Chrome); do not trust the executor's log.
  Also try a few flows outside the list (back navigation, an undo, a double click, a long
  input). The browser writes only to the isolated stack; this does not break the "no
  repo changes" rule. Leave the stack up when you are done: my test and `/fix` use it.
  Give the app URL in the report.
- Screenshots (UI feature): for each flow of the list, take 2 screenshots at scale 0.5
  with `save_to_disk`: light mode at 1024 x 600 and dark mode at full HD (a flow that
  names other sizes gets those). Use the `zoom` action for a detail (a chip, a border, a
  radius). Check each screenshot:
  - no clipped text, control, or tag;
  - the same height for the controls of one row;
  - the same corner radius for the same component;
  - no horizontal scroll and no overflow at 1024 x 600;
  - no overlay or tooltip clipped by its container;
  - readable in both themes.
  Each failure is a [CODE] finding with the screenshot name. Copy the saved files to
  `<feature folder>/screens/cycle-C-round-R/<flow>-<1024|fhd>-<light|dark>.png` and name
  the folder in the report.
- Judge the code as it is now, not by what you concluded in earlier rounds.

## Web access
Verify version-specific API usage, deprecations, and issues in any new dependency.
Cite the source URL for any finding based on external docs. Prefer official docs.

## review.md
Change review.md in place. Never append a section per round, and never repeat a closed
finding or a check that passed. Layout:
- First three lines: `Cycle: C` (copied from plan.md, 1 if missing), `Round: R`, and
  `Reviewed tree: <id>`. R is 1 when C differs from the previous review.md (or for a new
  review.md), else the previous R + 1.
- `## Open`: one block per open finding, with the heading
  `### F<n> <BLOCKER|MAJOR|MINOR> [<CODE|SPEC>] <title>` and the bullets `Status:` (`NEW`,
  `NEW (checkpoint <x>)`, or `REOPENED (<count>)`), `Where:` (file:line), `Problem:`, and
  `Fix:`. The executor adds `Addressed after round <R>: ...` under it.
  - [CODE]: the implementation is wrong; the executor can fix it within the plan.
  - [SPEC]: the spec or plan is wrong, incomplete, or contradicts the code.
- Each finding keeps its ID (`F1`, `F2`, ...) across rounds and cycles. When a finding that
  the executor marked "Addressed" is still wrong, set `Status: REOPENED (<count>)`.
- `## Closed`: one line per closed finding, moved from `## Open`:
  `*   F<n> <SEVERITY> [<TARGET>] <title>: <state>.` The state is `RESOLVED (cycle C, round R)`,
  `ADDRESSED IN SPEC (Step N)`, `FIXED INLINE (Artur, <date>)`, or `ACCEPTED (Artur, <date>)`
  when I decide to keep the code. A closed finding never comes back to `## Open`; a new
  defect at the same place gets a new ID.
- `## Rounds`: one line per round: `Cycle C, round R: tree <id>; <test areas run and
  results>; browser <passed>/<total> flows; screens <folder>.` Write no list of the
  checks that passed.
- `## Summary`: the summary table of `## Open`, then the verdict. `check-review-routing.ps1`
  fails when the table differs from the headings in `## Open`.
- The table, in exactly this form:
  ```
  | Severity | [CODE] | [SPEC] |
  |---|---|---|
  | BLOCKER | 0 | 0 |
  | MAJOR | 0 | 0 |
  | MINOR | 0 | 0 |
  ```
- Last line exactly: `Verdict: APPROVE` or `Verdict: CHANGES_REQUESTED`
  (any BLOCKER or MAJOR means CHANGES_REQUESTED).

Then give me the verdict and one line per open finding: severity, target, the problem in
words, file:line, and the fix, with the ID last. I can ask you to explain or reconsider
any finding.

## Handoff (cross-session messaging)
Run `pwsh .claude/check-review-routing.ps1 <review.md path>`. It prints the one allowed
target. Send one message to that target, by the result:
- `Verdict: APPROVE` → session `$ARGUMENTS-spec`: "Feature $ARGUMENTS: APPROVED in cycle C,
  round R. Remaining MINOR findings: <count>. See review.md."
- CHANGES_REQUESTED, and any finding is REOPENED (2) or more, or R is 4 or more →
  session `$ARGUMENTS-spec`: "Feature $ARGUMENTS: not converging at cycle C, round R
  (<finding IDs>). See review.md." Do not message the executor.
- CHANGES_REQUESTED with any open [SPEC] finding → session `$ARGUMENTS-spec`: "Feature
  $ARGUMENTS: spec-level findings in cycle C, round R. See review.md." Do not message the
  executor.
- CHANGES_REQUESTED with only [CODE] findings → session `$ARGUMENTS-executor`: "Feature
  $ARGUMENTS: review cycle C, round R requests code fixes - start a run. (If you don't have the
  executor instructions in context, follow .claude/commands/implement.md for feature
  $ARGUMENTS.) Feature folder: <absolute feature folder path>."
If the target session can't be found, tell me and give me the message to paste.
Then end your turn and wait for the next handoff.
