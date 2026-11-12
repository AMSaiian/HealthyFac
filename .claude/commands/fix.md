---
description: Inline fix - the planning session judges the blast radius of each fix, makes the small ones in the feature worktree, logs them, and rebuilds the verify slot. No plan step, no commit.
argument-hint: [<feature-name>] <what to change>
---
Fix: $ARGUMENTS

Make the listed fixes yourself, in the feature worktree, now. This call allows repo edits
for its items only. The rule "you do not edit code in the repo" of `/spec` stays for all
other work.

## Feature
- When the first word is the name of a folder in `<spec output root>/<project>/`, it is
  the feature, and the rest is the fix text. Otherwise use the feature of this session.
- The worktree is the checkout of branch `feature/<feature-name>` (`git worktree list`).
  When it does not exist, stop and ask me.
- When the open cycle of `plan.md` has an unticked step (`- [ ]`), or `review.md` routes an
  open finding to the executor, the executor can be working in the same worktree. Stop and
  ask me before you edit.

## Items
- Split the fix text into items. Restate each item in one line. Quote my words.
- Ask me only when an item has two readings that give different results.
- Read the code of each item first, then judge its blast radius: what else can break.

## Blast Radius
An item is inline only when all of these hold:
- **Every AC stays.** The fix makes the code match the spec, or changes only a look or a text.
- **No contract changes:** no request or response shape, route, permission, message, table,
  column, index, migration, options key, Compose setting, or package.
- **Local:** one use case, one component, or one store. No other caller sees a change.
  Shared code (`backend/src/Shared/`, `frontend/src/app/shared/`, a shared fixture, a rule,
  a hook) is not local.
- **No safety code:** no transaction, isolation level, retry, lock, row version, query
  filter, scope check, authorization, or cancellation path changes.
- **No proof line:** the fix needs no `Red:` or `Proof:` line (`testing.md`), because those
  go under a plan step.
- **Known cause:** I can name the cause from the code. A cause that needs an investigation
  or a reproduction is not inline.

For each item, give the verdict and its reason in one line before the change:
`inline - <reason>` or `plan - <the condition that fails>`. Make the inline items at once.
Do not make a plan item: propose its step in the report. When I answer "inline anyway" or
"plan it", follow my answer.

## Change
- Read each file before you change it. The repo rules of each path apply (`AGENTS.md`).
- Make the smallest change that does the item. Change nothing outside the items.
- Run the tests of each changed area:
  - Frontend: `cd frontend && npx ng test --watch=false --include <folder>`.
  - Backend: `dotnet test --project <test project> --filter <filter of the changed use case>`.
- The Stop hook formats, builds, and lints the changed files: fix each item that it reports.

## Verify slot
- Run `pwsh scripts/verify-stack.ps1 up` in the worktree, in the background, with its
  output in a log file in the scratchpad. On a running slot, Docker rebuilds only the
  changed images and keeps the volumes.
- Never run `down` between fixes: it removes the volumes and the images. The data that
  the reviewer or I left is fine for a fix check.
- Run `down`, then `up`, only when the slot is stale: the diff since the slot started
  changes `Migrations/`, `Seeding/`, or `docker/keycloak/realm/` (an edited migration or
  a changed seed does not apply to an existing database), or the slot started on an
  earlier day (the seeded work orders and invites are tied to the seed day). Tell me why.
- The log holds the sign-in password. Never repeat it. Give me the log path.

## Log
- `plan.md`, `## Deviations`, one entry per call:
  `*   Inline fix, cycle N (Artur, YYYY-MM-DD: "<my words>"): <each change with its files>.`
  N is the `Cycle:` line. The words "cycle N" let `check-handoff.ps1` count the files.
- `spec.md`: when an item changes what a line describes (a layout, a text, a control),
  update that line with my words and the date.
- `review.md`: mark each finding that an item fixes `Fixed inline (Artur, YYYY-MM-DD)`.
- Run `pwsh <worktree>/.claude/check-plan.ps1 <feature folder>` and fix each line it prints.

## Review
- Do not message the reviewer. When I ask for a review of the fixes, relay my words to
  `<feature-name>-reviewer`, marked as a relay, with the list of changed files.

## Report
- One table: item, verdict, files, check result. Then the plan items, each with the failed
  condition and the proposed step. Then the app URL and the log path.
- End with: "Check in the browser and tell me 'tested' or the next fix."
- Never commit. Commit only when I ask.
