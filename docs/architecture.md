# HealthyFac Architecture

This document records the service boundaries and the decisions that connect the services. Read `README.md` for the product domain.

## Services

| Service | Owns | Status |
| --- | --- | --- |
| **Gateway** (YARP) | The only public entry point. Routing, TLS, rate limiting, the edge JWT check, and the token revocation check. | Exists |
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

## Messaging

- **Transport:** Rebus sends one message type, `IntegrationEnvelope`. It holds the message type name and the JSON payload.
- **Broker:** all topics share one RabbitMQ topic exchange, `RebusTopics`. The routing key is the message type name.
- **Queue:** each service owns one input queue (`Messaging:QueueName`). It subscribes to each message type that the service handles.
- **Outbox processor:** it locks one pending row with `FOR UPDATE SKIP LOCKED`, publishes it, and marks it in one transaction. The row ID is the Rebus message ID. A run ends when no row is due or when a publish fails. The next run starts after the polling interval.
- **Inbox:** a Rebus handler stores each delivery in the inbox. A duplicate message ID creates no second row. The insert runs under the retrying execution strategy of the service context, so a short database outage during delivery is retried inside each Rebus attempt.
- **Inbox processor:** it locks one row per transaction, restores the actor, and calls `IIntegrationMessageHandler<TMessage>`. A failure rolls back the transaction and schedules a retry.
- **Inbox attempt limit:** an inbox row stops after `Messaging:InboxMaxAttemptCount` failed attempts (default 10, about 13 minutes) and gets `failed_on`. The processor never claims it again and logs one `Error` entry for it. The outbox has no limit, because its failures are broker outages.
- **Run a failed row again:** after a fix, set `failed_on = NULL`, `attempt_count = 0`, and `next_attempt_on = now()` for that row.
- **Retry delay:** the delay doubles from two seconds and stops at five minutes.
- **Delivery:** the guarantee is at least once. A crash after the publish and before the commit sends a message twice. The inbox drops the duplicate.
- **One handler:** each message type has exactly one handler per service. A second registration of the same type throws at start-up. When two steps must react to one message, one handler runs both, or it saves a new message through the outbox.
- **Broker health:** `/health/ready` checks RabbitMQ on one cached connection. A failure reports `Degraded` with status `200`, because the outbox accepts writes while the broker is down. The check times out in 3 seconds, below the gateway probe timeout.
- **Throughput and backlog:** both processors handle one message per transaction. This is enough for business events. Before production, add OpenTelemetry metrics for each processor: the pending row count and the age of the oldest pending row. When the age grows under normal load, change the outbox processor to claim a batch of rows in one transaction. The inbox stays one message per transaction, so a failed handler rolls back only its own message.
- **Actor headers:** `healthyfac-actor-id`, `healthyfac-company-id`, and `traceparent`. A message without a valid actor header, message ID, or company header goes to the `error` queue at the first attempt. No retry can fix it.
- **Delivery retries:** other delivery failures get 20 attempts, and each attempt retries transient database failures, so a database outage of about 10 minutes loses no message. The `error` queue is final: a message in it is lost.
- **Error logs:** each move to the `error` queue writes an `Error` log entry with the message ID. Rebus logs reach the application logger without extra configuration.

## Actor Context

- Each DI scope has one actor: the user ID, the company ID, the employee ID, and a system flag. `ICurrentUserProvider` reads it.
- An HTTP request, an inbox message, or a job enters the actor once. A second enter throws until the first lease ends.
- A scope outside an HTTP request starts with `CreateActorScope`. It is the only way to start a job scope, a processor step, or the database start-up.
- Only Infrastructure and Web can set the actor. `ActorContextHolder` lives in Shared.Infrastructure, so an Application handler cannot replace the actor.
- The system actor has a fixed GUID. It is not a Keycloak user. Until authentication exists, every HTTP request runs as the system actor.
- `Outbox.Add` stores the actor in the row. The inbox processor restores the original actor, so audit fields show who started the work.

## Public Paths

- The public path has the form `/api/<service>/v{version}/...` (e.g., `/api/core/v1/machines`).
- A service routes under `/v{version}`, without `/api`. The gateway removes the whole `/api/<service>` prefix.
- The gateway sends `X-Forwarded-Prefix: /api/<service>`. A service accepts this header from trusted proxies only (`Proxy:KnownNetworks`). Its `PathBase` then becomes the public prefix, so every link that it builds (e.g., the `Location` header) shows the public path.

## Gateway Failure Policy

- Redis is not a hard dependency of the gateway. When Redis fails, rate limiting falls back to a local limiter per instance. With N instances, the effective limit can reach N times the configured limit during an outage.
- A circuit breaker skips Redis for 30 seconds after a failure. Then one request tests Redis, and the others keep the local limiter until the test succeeds. The gateway logs one warning when the breaker opens and one when it closes.
- `/health/ready` reports `Degraded` with status `200` while Redis is down. The gateway readiness does not check the services behind it.
- Every error response of the gateway is `application/problem+json`: `429` (rate limit), `502` (destination unreachable or broken response), `503` (no healthy destination), `504` (destination timeout), and `500` (gateway exception, without details). A client cancellation gets no body.
- The gateway never retries a request. A `POST` request is not idempotent, so the client decides on a retry.
- The gateway skips a destination that the health checks mark unhealthy. It does not fall back to all destinations.

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
