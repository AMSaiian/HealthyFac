# HealthyFac Architecture

This document records the service boundaries and the decisions that connect the services. Read `README.md` for the product domain.

## Services

| Service | Owns | Status |
| --- | --- | --- |
| **Gateway** (YARP) | The only public entry point. Routing, TLS, rate limiting, the edge JWT check, and the token revocation check. | Planned |
| **Auth** | A thin back channel to Keycloak: user provisioning, organization membership, and role assignment through the Keycloak Admin API. It does not log users in or handle passwords. It may have its own tables when it needs them. | Planned |
| **Core** | Business data: companies, departments, employees, employee invites, roles, machines, metric definitions, and campaigns. | Exists |
| **Telemetry** | Metric intake from the machines (a mock machine service for now). It writes the received data and nothing else. | Planned |
| **Insights** | Document ingestion, the LLM worker, and the statistic rules that detect anomalies. | Planned |
| **Document** | A utility service for file storage on Azurite. | Planned |

## Data Ownership

- One PostgreSQL server holds one database per service (e.g., `healthyfac_core`, `healthyfac_auth`, `keycloak`).
- A service writes only to its own database.
- **Exception:** Telemetry is the only writer of the telemetry database. Insights reads it for now. Revisit this when the warnings feature is designed.
- Core is the source of truth for companies and employees. Other services get the data they need from messages or from the token claims.

## Communication

- **Client to service:** the SPA calls the gateway. The gateway forwards to the service. Every service validates the JWT again.
- **Service to service, asynchronous:** RabbitMQ with Rebus. Every service uses the transactional outbox and inbox (`backend-infrastructure.md`, rule **Outbox and Inbox**). A message type name has the form `<service>.v<version>:<Name>` (e.g., `core.v1:EmployeeInvited`).
- **Service to service, synchronous:** OAuth 2.0 Token Exchange when the call acts for a user, or Client Credentials when the service acts for itself. Never put a token into a message.

## Identity

- Keycloak is the identity provider. The SPA signs in with the Authorization Code flow and PKCE.
- Each company is a Keycloak organization. The organization alias is `Company.Id`, so Keycloak holds no other company data.
- The access token carries `sub` (the Keycloak user ID), the permissions, the `organization` claim (the company ID), and `employee_id`.
- The roles `worker` and `supervisor` group the permissions. API policies check permissions, never role names.
- Today, maintenance workers and supervisors sign in. Sign-in for other users (e.g., a platform operator outside every company) is not decided.

## Shared Infrastructure

- **Redis:** the token revocation list, the gateway rate limiter counters, and the second level of the distributed cache. Redis holds no source data and no tokens.
- **Azurite:** blob storage for the Document service in development.

## Open Decisions

| Topic | State |
| --- | --- |
| Sign-in for users outside a company (e.g., a platform operator) | Deferred |
| Telemetry database access for the warnings feature | Discuss when the warnings feature starts |
