# Compose Settings Files

Each file `<service>.json` holds the fixed settings of one .NET service inside the Compose network. Compose mounts it read-only as `/app/appsettings.Deployment.json`. The service loads it through `AddDeploymentSettingsFile()` (`HealthyFac.Shared.Web`).

## Where a Setting Belongs

| Setting | Place |
| --- | --- |
| The same value in every environment (a queue name, a client ID, a limit) | `appsettings.json` of the service |
| A value of a run on the host (a `localhost` address, the development key ring metadata) | `appsettings.Development.json` of the service |
| A fixed value of the Compose network that is no secret (an in-network address, the mail and telemetry endpoints, the gateway destinations) | `docker/config/<service>.json` |
| A secret (a key, a password, a client secret, a connection string with a password) | `.env`, passed by Compose as an environment variable |
| A value of this machine (a host port, the public domain and URLs, a file path, a switch) | `.env`, passed by Compose as an environment variable |

## Priority

A later source overrides an earlier one:

1.  `appsettings.json`
2.  `appsettings.<Environment>.json`
3.  `appsettings.Deployment.json` (this folder)
4.  Environment variables

So a value in `docker-compose.yml` or in `docker/verify/docker-compose.verify.yml` still overrides a file of this folder. The verify stack uses that for the certificate paths of the gateway.

## Rules

*   Never put a secret in a file of this folder. The files are committed.
*   Never add a fixed value with `__` in its name to the `environment` of a .NET service in `docker-compose.yml`. Put it here. `scripts/tests/verify-slots.tests.ps1` checks it.
*   Compose cannot fill a variable into these files. A value that depends on `.env` stays in `environment`.
*   `document.json` holds the well-known development account of Azurite and its published key. They are public by design, and Azurite accepts no other account. A real storage account gets its connection string as a secret.
*   Another deployment (e.g., a cloud environment) mounts its own file at the same path, or sets environment variables.
