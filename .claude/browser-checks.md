# Browser Checks
The executor, the reviewer, the spec writer, and `/fix` follow this file for every browser flow on a verify slot.
`check-handoff.ps1` checks the flow lines; `check-plan.ps1` checks the `Look:` lines.

## Agent Chrome
- Use only the agent Chrome: a second Chrome process with its own user data folder and switches
  (`scripts/agent-chrome.ps1`). The subagents use the browser of the main session.
- Before the first flow of a run, and after each screenshot timeout, run `pwsh scripts/agent-chrome.ps1 restore`. A
  minimized window draws nothing, so each screenshot times out, and the page still reports `visible`. A covered
  window and a dark display work.
- Exit 1 of `restore` (the agent Chrome does not run): the flows wait (**Errors**).

## Sizes
- `1280 x 600` and `1920 x 1080` are window sizes (`resize_window`). A maximized window ignores a resize: the page
  size (`innerWidth`, `innerHeight`) must change with it.
- A resize reaches only the active tab: take a screenshot of the tab first.
- After `restore` and after each `resize_window`, open a new tab for the flow: a tab that was open before keeps its old
  page size. Check that `innerWidth` and `innerHeight` follow the window size before the first screenshot. A page that
  does not follow is no pass.
- Never fake a size (a root `zoom`, a scaled frame, an emulated viewport).

## Looking
- A view passes only from a screenshot that worked. Take it at scale 1, and use `zoom` for a detail (a chip, a
  border, a radius). Numbers from a script (`getBoundingClientRect`) support a check, never replace the screenshot.
  Save no screenshot files.
- Check each view in light at 1280 x 600 and in dark at 1920 x 1080 (a flow that names other sizes gets those):
  - no clipped text, control, or tag;
  - the same height for the controls of one row;
  - the same corner radius for the same component;
  - no horizontal scroll and no overflow at 1280 x 600;
  - no overlay or tooltip clipped by its container;
  - readable in both themes, and distinct from its background;
  - the same look as the `Look:` reference of the step: open it (`git show develop:<file>`, or the named page on the
    slot) and compare the same component (gaps, radius, size, font, turn, color).

## Drags
- Drag with the `left_click_drag` action. It is a real mouse drag, so a layer on top blocks it as it blocks a user.
- A script may replace only what the action cannot do: a hold (a dwell), or a view during the drag. It sends the
  events to `document.elementFromPoint(x, y)` at the pointer point, never to an element that a selector found. Never
  replace a browser function (e.g., `setPointerCapture`).
- For the view during a drag (the preview, the shadow, the drop marker): send `mousedown` and `mousemove` to the
  element at the point, take a screenshot, then send `mouseup`.

## Flow Lines
- One line per flow under its step: `pass: <what you saw>; seen: <size> <theme>, ...` or
  `fail: <what you saw>`. A later round adds `round <R>: ...` lines.
- `seen:` names each size and theme with a screenshot that worked.
- A flow that did not run as written is no `pass`: another size, a script drag at a selector, a faked size, a replaced
  browser function, a timed-out screenshot, or "not checked". Write `fail` with the reason, or wait (**Errors**).

## Errors
- A transient error (a screenshot timeout, a slot that does not start, a tool call that times out, a flaky test):
  fix what you can (`restore`, `up`), then run the same command once more. Name it in the line (`retried: <what>`).
- Before a `BLOCKED` or a `WAITING` for the browser, triage in this order: (1) read `outerWidth`, `outerHeight`,
  `innerWidth`, and `innerHeight` on the page: a tiny outer size means a minimized window; an inner size that does not
  follow the window means a maximized window or an old tab; (2) run `pwsh scripts/agent-chrome.ps1 restore`; (3) open a
  new tab; (4) test on an app host (`https://app.s<n>.hfverify.test` or `https://app.healthyfac.test`): the extension
  has site access only to the HealthyFac hosts, so a page like example.com proves nothing; (5) check with
  `list_connected_browsers` that the agent Chrome is the browser in use. Write each step taken into the `retried:` part.
- It fails again, or the environment blocks the step (Docker down, no slot, the agent Chrome not reachable): the
  executor marks the browser check step `[!] ... WAITING: <error>; retried: <what>`. The cycle goes on, the executor
  tries the step again at the start of each run, and the last cycle commits only after its flows pass
  (`commit-cycle.ps1`). The reviewer reports `browser waiting` and files no finding for a waiting step with `retried:`.
  Never plan a deferred or manual-only browser check.
- A permission denial is no transient error. Never run the command in another form. Report it; the main session runs
  it.
