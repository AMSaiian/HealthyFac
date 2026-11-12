# Local Setup

## New Worktree

Run an agent session in its own worktree, so sessions never share changed files.

1.  Create the worktree: `git worktree add ../HealthyFac-wt2 -b <branch> develop`.
2.  Install the frontend packages: `cd frontend && npm ci`.
3.  Start nothing else. A worktree needs no `.env`: `scripts/verify-stack.ps1` uses the `.env` of the main checkout. A `.env` in the worktree takes precedence.

`ng serve` uses the fixed port 4200, so run it in one worktree at a time.

## Dev Stack

Only the main checkout runs `docker compose up` for the dev project `healthyfac`. A worktree never does, because the container names and the ports are fixed.

## Verify Stacks

Each worktree checks the UI in its own verify stack (`pwsh scripts/verify-stack.ps1 up`).

*   The script gives the worktree a slot from 1 to 4. Slot `n` is the Compose project `healthyfac-verify-<n>`, the domain `s<n>.hfverify.test`, the gateway port `9443 + n`, and the Mailpit port `8026 + n`. The stack also publishes Grafana on `3000 + n`, the RabbitMQ management UI on `15672 + n`, and the Keycloak admin console on `8180 + n`, all on `127.0.0.1`. `up` prints the URLs.
*   `status` and `down` act on the slot of the worktree only. `-Slot <n>` names a slot, and the script refuses a slot that another worktree owns.
*   `up` claims the slot with the lock file `slot<n>.lock` in the folder `healthyfac-verify-slots` of the git common folder, so two `up` runs at the same time get different slots. `down` removes the lock file. A crashed `up` keeps its slot until `down`. The script removes a lock file whose worktree folder is gone and whose slot has no container.
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
