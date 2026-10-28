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
- **Inbox processor:** it locks one row per transaction, restores the actor, and calls the use case `IRequestHandler<TMessage>` through the validation and logging decorators. A failure rolls back the transaction and schedules a retry.
- **Inbox attempt limit:** an inbox row stops after `Messaging:InboxMaxAttemptCount` failed attempts (default 10, about 13 minutes) and gets `failed_on`. The processor never claims it again and logs one `Error` entry for it. The outbox has no limit, because its failures are broker outages.
- **Run a failed row again:** after a fix, set `failed_on = NULL`, `attempt_count = 0`, and `next_attempt_on = now()` for that row.
- **Retry delay:** the delay doubles from two seconds and stops at five minutes.
- **Delivery:** the guarantee is at least once. A crash after the publish and before the commit sends a message twice. The inbox drops the duplicate.
- **One use case:** each message type has exactly one use case per service. `AddUseCases` throws at start-up when one request has two handlers. When two steps must react to one message, one use case runs both, or it saves a new message through the outbox. A use case without a result derives from `RequestHandler<TRequest>`, and a caller injects `IRequestHandler<TRequest>`: an internal adapter calls the decorated pipeline, so validation and logging always run.
- **Broker health:** `/health/ready` checks RabbitMQ on one cached connection. A failure reports `Degraded` with status `200`, because the outbox accepts writes while the broker is down. The check times out in 3 seconds, below the gateway probe timeout.
- **Throughput and backlog:** both processors handle one message per transaction. This is enough for business events. Before production, add OpenTelemetry metrics for each processor: the pending row count and the age of the oldest pending row. When the age grows under normal load, change the outbox processor to claim a batch of rows in one transaction. The inbox stays one message per transaction, so a failed handler rolls back only its own message.
- **Actor headers:** `healthyfac-actor-id`, `healthyfac-company-id`, and `traceparent`. A message without a valid actor header, message ID, or company header goes to the `error` queue at the first attempt. The `error` queue holds only messages that the inbox cannot store: these header faults and a payload that `jsonb` cannot store (see **Invalid messages**). No retry can fix them.
- **Invalid messages:** a payload that does not deserialize, or that fails the validator of its contract, gets `failed_on` at the first attempt, with no retries, and the processor writes one `Error` log entry. The payload stays in the database, where it can be queried and run again. The inbox key drops every later copy of the same message ID, so one invalid message gives one row and one log entry. Other rejections (e.g., a missing entity) keep the normal retries, because message order is not guaranteed. A payload that `jsonb` cannot store (not JSON, or a `\u0000` escape in a string) goes to the `error` queue at the first attempt, because the insert would fail on every attempt.
- **Delivery retries:** other delivery failures get 20 attempts, and each attempt retries transient database failures, so a database outage of about 10 minutes loses no message. The `error` queue is final: a message in it is lost.
- **Error logs:** each move to the `error` queue writes an `Error` log entry with the message ID. Rebus logs reach the application logger without extra configuration.
- **Tracing:** the `HealthyFac.Messaging` source writes the spans `send`, `receive`, and `process`. Each span is the parent of the next, so one trace goes from the HTTP request through the handler. The inbox logs `Information` for each handled message. An SQL span is recorded only under a parent span (`ConfigureParentOnlyTracing`), so the idle polls of the processors create no trace. Processor health comes from metrics (Session 8).

## Actor Context

- Each DI scope has one actor: the user ID, the company ID, the employee ID, and a system flag. `ICurrentUserProvider` reads it.
- An HTTP request, an inbox message, or a job enters the actor once. A second enter throws until the first lease ends.
- A scope outside an HTTP request starts with `CreateActorScope`. It is the only way to start a job scope, a processor step, or the database start-up.
- Only Infrastructure and Web can set the actor. `ActorContextHolder` lives in Shared.Infrastructure, so an Application handler cannot replace the actor.
- The system actor has a fixed GUID. It is not a Keycloak user. An anonymous HTTP request runs as the system actor.
- An authenticated HTTP request runs as the user of its token. `ActorContextMiddleware` runs after authentication and reads `sub`, `organization`, and `employee_id`. Each value must be a GUID, and `organization` must hold exactly one value. Any other shape returns `401`, because the tenant is not clear.
- `Outbox.Add` stores the actor in the row. The inbox processor restores the original actor, so audit fields show who started the work.

## Email

- A service sends email through `IEmailSender` (Shared.Application). The host picks the implementation with one registration method. `AddSmtpEmail` registers the SMTP sender (MailKit). A later provider adds its own `Add...Email` method.
- Development uses Mailpit. It catches all email, and its web UI shows the messages (`MAILPIT_UI_PORT`, default `8025`). Mailpit keeps the messages in the `mailpit-data` volume, and `docker compose down -v` removes them. Compose sets the `Smtp__*` variables of `core`.
- Other deployments set `Smtp__Host`, `Smtp__Port`, `Smtp__Security`, `Smtp__UserName`, `Smtp__Password`, `Smtp__FromAddress`, and `Smtp__FromName`.
- **Invite email:** Core consumes `core.v1:EmployeeInvited` and emails the invited person the link `{InviteLinks:SpaBaseUrl}/invite#token={token}`. The token is in the fragment, so no server log or proxy records it.
- **Rendering:** the email bodies are Razor components in the Web layer. `RazorComponentRenderer` (`Shared.Web/Rendering`) renders them with `HtmlRenderer`, outside an HTTP request. It is not email-specific: a later feature can reuse it for other output. The Application layer sees only `IEmployeeInviteEmailBuilder`. The text body is the HTML-decoded render of its own component.
- **Spans:** the `HealthyFac.Rendering` source writes `Razor.Render` for each rendered body. The `HealthyFac.Email` source writes `Email.Send` for each message. The MailKit source `MailKit.Net.SmtpClient` adds the SMTP steps under `Email.Send`. No span holds a recipient, a body, or a link.
- **Token at send time:** the handler builds the token from the current salt of the invite. The token never enters the database, the outbox, or a log. Two messages for one invite send two links that both work.
- **Skipped invites:** the handler sends nothing and completes when the invite is missing, accepted, or expired. A supervisor resends an expired invite.
- **Failures:** a sender failure makes the inbox retry the message. After `Messaging:InboxMaxAttemptCount` attempts, the inbox row gets `failed_on`. A crash after the send and before the commit sends the email again.

## Public Paths

- The public path has the form `/api/<service>/v{version}/...` (e.g., `/api/core/v1/machines`).
- A service routes under `/v{version}`, without `/api`. The gateway removes the whole `/api/<service>` prefix.
- The gateway sends `X-Forwarded-Prefix: /api/<service>`. A service accepts this header from trusted proxies only (`Proxy:KnownNetworks`). Its `PathBase` then becomes the public prefix, so every link that it builds (e.g., the `Location` header) shows the public path.
- **Logical names:** each public service has its own name, so a move to the cloud changes only the names and the scheme. The domain is `healthyfac.test` (`HEALTHYFAC_DOMAIN`). The `.test` top-level domain is reserved for tests (RFC 6761) and never resolves on the internet.

| Name | Serves | Cloud equivalent |
| --- | --- | --- |
| `https://api.healthyfac.test` | the gateway, Core API routes (`/api/core/v{n}/...`) | `https://api.<domain>` |
| `https://auth.healthyfac.test` | the gateway, Keycloak routes (`/realms/healthyfac/...`, `/resources/...`); the token issuer | `https://auth.<domain>` |
| `https://app.healthyfac.test:4200` | the web app (not implemented yet); invite links, redirect URIs, CORS origin | `https://app.<domain>` |

- **TLS:** TLS ends at the gateway, as at a cloud load balancer or ingress.
    - The public URLs use HTTPS and no port. `GATEWAY_PORT` is 443; when it is not, add the port to `HEALTHYFAC_AUTH_URL`, because the issuer must equal the URL that the browser uses.
    - The gateway publishes HTTPS only. Plain HTTP on the public port gets no HTTP response.
    - Internal traffic stays HTTP: gateway to Core, gateway to Keycloak, the health ports, and the Keycloak admin console on `localhost`. The gateway sets `X-Forwarded-Proto: https`, and Core and Keycloak (`KC_PROXY_HEADERS: xforwarded`) read it.
    - A local certificate authority (mkcert) signs the certificate. The files are `docker/certs/healthyfac.test.pem` and `docker/certs/healthyfac.test-key.pem`. Git ignores them, and no image holds them. Compose mounts the folder read-only into the gateway. See `docker/certs/README.md`.
    - The launch profile `https` of the gateway serves `https://localhost:5001` with the same files. With the hosts file, a browser reaches `https://api.healthyfac.test:5001`.

- **Host routing:** each gateway route has a YARP `Match.Hosts` entry with its name. A host without a port matches every port. A request to another name matches no route: `401` without a token, `404` with a token. The names are set per environment (`appsettings.Development.json`, Compose); `appsettings.json` holds none.
- **Development tools** stay on `localhost`: the Keycloak admin console (`KEYCLOAK_PORT`, set by `KC_HOSTNAME_ADMIN`), Mailpit, Grafana, the RabbitMQ console, the database, and the health ports.
- **Hosts file:** map the three names to the local machine, as an administrator (`C:\Windows\System32\drivers\etc\hosts` on Windows):

```text
127.0.0.1 api.healthyfac.test auth.healthyfac.test app.healthyfac.test
```

- **CORS at the gateway only:** the gateway answers the cross-origin requests of browsers (`AddSharedCors`, `UseSharedCors`).
    - Each route that the web app calls (`core`, `core-invite-public`) names the policy with `"CorsPolicy": "HealthyFacDefault"`.
    - The CORS middleware runs before authentication. A preflight (`OPTIONS`, no `Authorization` header) gets `204`. It never meets authentication, the revocation check, the rate limiter, or authorization.
    - `Cors:PreflightMaxAgeSeconds` (600) lets the browser cache the answer. Browsers ignore a value above 7200.
    - A service behind the gateway keeps `Cors:Enabled = false`. Two layers that write `Access-Control-Allow-Origin` make the browser reject the response.
    - Keycloak answers CORS for its own routes from the `webOrigins` of each client. The gateway adds no CORS headers there.
    - The allowed origins have no default. Each environment sets `Cors:AllowedOrigins` (Compose: `HEALTHYFAC_SPA_URL`).
- **Versioned paths only:** the `core` route matches `/api/core/v{n}/...` only. Paths such as `/api/core/hc`, `/api/core/health/ready`, and `/api/core/openapi/...` match no route, so the gateway makes no call to Core. It answers `401` without a token and `404` with a token.

## Health Endpoints

- Each service serves `/health` (liveness), `/health/ready` (readiness), and `/hc` (every check, with exception texts) on one internal health port.
    - The setting is `HealthEndpoints:Port`. The containers use `8090`. The API port does not serve these paths.
- `UseSharedHealthChecks()` is the first middleware. It compares the local port of the connection, which a client cannot change. It never reads the `Host` header.
- Health requests skip HTTPS redirect, request logging, CORS, authentication, revocation, rate limiting, and authorization.
- Compose publishes the health ports on `127.0.0.1` only (`CORE_HEALTH_PORT`, `GATEWAY_HEALTH_PORT`). No gateway route reaches them. In the cloud, the platform probes use the health port, and no public load balancer routes it.
- The gateway active health check of Core calls the `Health` address of the destination (the health port of Core), with the path `/health/ready`.

## Gateway Failure Policy

- Redis is not a hard dependency of the gateway. When Redis fails, rate limiting falls back to a local limiter per instance. With N instances, the effective limit can reach N times the configured limit during an outage.
- A circuit breaker skips Redis for 30 seconds after a failure. Then one request tests Redis, and the others keep the local limiter until the test succeeds. The gateway logs one warning when the breaker opens and one when it closes.
- `/health/ready` reports `Degraded` with status `200` while Redis is down. The gateway readiness does not check the services behind it.
- Every error response of the gateway is `application/problem+json`: `429` (rate limit), `502` (destination unreachable or broken response), `503` (no healthy destination), `504` (destination timeout), and `500` (gateway exception, without details). A client cancellation gets no body.
- The gateway never retries a request. A `POST` request is not idempotent, so the client decides on a retry.
- The revocation check passes a request when Redis fails, and the same circuit breaker protects it.
- The gateway skips a destination that the health checks mark unhealthy. It does not fall back to all destinations.

## Identity

- Keycloak is the identity provider. The SPA signs in with the Authorization Code flow and PKCE.
- Each company is a Keycloak organization. The organization alias is `Company.Id`, so Keycloak holds no other company data.
- The access token carries `sub` (the Keycloak user ID), the permissions, the `organization` claim (the company ID), and `employee_id`.
- The roles `worker` and `supervisor` group the permissions. API policies check permissions, never role names.
- **Realm as code:** `docker/keycloak/import/healthyfac-realm.json` is the development realm. Keycloak imports it at start (`--import-realm`) and replaces each `${NAME}` placeholder with the environment variable of the same name. The import skips a realm that already exists. After a change, run `docker compose down -v`, or delete the realm in the admin console. Production realm provisioning is not decided. Edit the user profile in `docker/keycloak/user-profile.json` only, then run `docker/keycloak/embed-user-profile.sh`, which embeds it into the realm file as the string that Keycloak expects; the test `UserProfile_InRealmFile_EqualsSourceFile` fails when the two differ.
- **Clients:** `healthyfac-spa` (public, Authorization Code with PKCE), `healthyfac-core` (confidential, holds the permission roles), and `healthyfac-auth` (service account for the Auth service). The client has the scope `service_account` explicitly, because the realm import does not add it. A service token carries `client_id` (it identifies the calling service) and no `clientHost` or `clientAddress`, so it holds no host name or IP address of the caller.
- **Permissions:** client roles of `healthyfac-core`: `employees:invite`, `employees:manage`, and `machines:write`. The client holds permissions only. The job roles `supervisor` (all three) and `worker` (none yet) are realm roles, composites of those client roles, so a job role can later also hold permissions of other APIs. The mapper `healthyfac-core-permissions` of the scope `healthyfac-claims` writes the effective client roles of `healthyfac-core` into the flat claim `permissions`. The JWT handler splits the array into one `permissions` claim per value, and the endpoint policies check that claim. The name of a policy is the name of the permission. The token has no `resource_access` and no `realm_access`.
- **Token validation:** every service validates the JWT itself (`AddSharedJwtAuthentication`). The issuer is the public URL of the realm (the gateway). The service reads the keys from the internal Keycloak address (`Jwt:MetadataAddress`), which works because of `KC_HOSTNAME_BACKCHANNEL_DYNAMIC`. A token needs `healthyfac-core` in `aud`. The service reads the permissions from the `permissions` claim and needs no claims transformation. The access token lives 300 seconds.
- **Core endpoints:** a fallback policy requires a signed-in user. Invite create and resend need `employees:invite`, and machine create needs `machines:write`. Verify, accept, and OpenAPI (Development only) are anonymous. The health endpoints are on the health port (see **Health Endpoints**). The company of a request comes from the token only: a Web request record has no `CompanyId`.
- **Gateway:** the gateway validates the token, checks the revocation list, and then applies the rate limit. It routes `/realms/healthyfac/*` and `/resources/*` to Keycloak. The Keycloak routes answer only `auth.<domain>`, and the Core routes answer only `api.<domain>`. No route reaches `/admin`. The `core` route needs a signed-in user, and the default rate limit counts for each user (`sub`). A fallback policy (`SetAuthenticatedFallbackPolicy`) requires a signed-in user for a route without `AuthorizationPolicy` and for a request that matches no route. Public routes name the `anonymous` policy.
- **Revocation:** the key `revoked:{sub}` holds the revocation time in Unix seconds. The gateway answers `401` when `iat` is earlier. A missing entry passes. Redis failures follow the **Gateway Failure Policy**: the request passes, and the circuit breaker logs a warning.
- **Test tenant:** one set of `.env` values (`HEALTHYFAC_TEST_*`) feeds the realm import and the Core development seed. The seed creates the company, two departments, and an active supervisor with the fixed IDs, and no `EmployeeAccepted` message.
- Today, maintenance workers and supervisors sign in. Sign-in for other users (e.g., a platform operator outside every company) is not decided.

## Shared Infrastructure

- **Redis:** the token revocation list, the gateway rate limiter counters, and the second level of the distributed cache. Redis holds no source data and no tokens.
- **Azurite:** blob storage for the Document service in development.

## Open Decisions

| Topic | State |
| --- | --- |
| Sign-in for users outside a company (e.g., a platform operator) | Deferred |
| Telemetry database access for the warnings feature | Discuss when the warnings feature starts |
