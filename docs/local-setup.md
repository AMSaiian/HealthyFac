# Local Setup

## Feature Worktree

The sessions of a feature run in its own worktree. Claude Code takes the Stop hook and the rules from the checkout in which a session starts, and the hook verifies the checkout of its own script file. A session that starts in the main checkout never formats, builds, or lints the files of a worktree.

1.  Start the feature: `pwsh .claude/start-feature.ps1 <feature>`. It creates the worktree `../HealthyFac-worktrees/<feature>` on the branch `feature/<feature>` from `develop`, and opens the spec, executor, and reviewer sessions there. `-Resume` opens the sessions of an existing feature again.
2.  Install the frontend packages in the worktree: `cd frontend && npm ci`.
3.  Start nothing else. A worktree needs no `.env`: `scripts/verify-stack.ps1` uses the `.env` of the main checkout. A `.env` in the worktree takes precedence.

`ng serve` uses the fixed port 4200, so run it in one worktree at a time.

## Dev Stack

Only the main checkout runs `docker compose up` for the dev project `healthyfac`. A worktree never does, because the container names and the ports are fixed.

## Verify Stacks

Each worktree checks the UI in its own verify stack (`pwsh scripts/verify-stack.ps1 up`).

*   The script gives the worktree a slot from 1 to 4. Slot `n` is the Compose project `healthyfac-verify-<n>`, the domain `s<n>.hfverify.test`, the gateway port `9443 + n`, and the Mailpit port `8026 + n`. The stack also publishes Grafana on `3000 + n`, the RabbitMQ management UI on `15672 + n`, and the Keycloak admin console on `8180 + n`, all on `127.0.0.1`. `up` prints the URLs.
*   The script acts on the checkout that holds the script file, not on the current folder: run `scripts/verify-stack.ps1` of the worktree whose stack you want.
*   `status`, `stop`, `reset`, and `down` act on the slot of the worktree only. `-Slot <n>` names a slot, and the script refuses a slot that another worktree owns.
*   `stop` stops the containers and keeps the data. `reset` removes the containers and the volumes and keeps the images, so the next `up` starts on clean data and rebuilds only the changed images. `down` also removes the images and the slot.
*   `up` claims the slot with the lock file `slot<n>.lock` in the folder `healthyfac-verify-slots` of the git common folder, so two `up` runs at the same time get different slots. `down` removes the lock file. A crashed, stopped, or reset stack keeps its slot until `down`. The script removes a lock file whose worktree folder is gone and whose slot has no container, so run `down` before you remove a worktree with a stopped stack.
*   The hosts file needs the 12 names of the 4 slots, and the certificate is a wildcard certificate. See `docker/certs/README.md`, **Verify stack**.
*   The script warns when less than 3 GiB of Docker memory is free. Four stacks and the dev stack need a large Docker memory limit.
*   The script checks its pure rules with `pwsh scripts/tests/verify-slots.tests.ps1`.

## Docker Address Pool

Docker Desktop gives each network a `/16` subnet from `172.x` by default. Test runs and verify stacks create many networks, and the pool runs out ("all predefined address pools have been fully subnetted").

Set a larger pool in `~/.docker/daemon.json` and restart Docker Desktop:

```json
{ "default-address-pools": [{ "base": "10.200.0.0/16", "size": 24 }] }
```

The pool gives 256 networks of 256 addresses each.
