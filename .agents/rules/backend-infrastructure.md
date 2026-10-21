---
trigger: glob
globs: "**/*.Infrastructure/**"
description: "Rules for the Infrastructure layer."
---
# Infrastructure Layer Rules
*   **ORM:** Use Entity Framework Core.
*   **Datetimes:** Use the BCL types. Do not add NodaTime.
    *   **Past Instants:** Store a moment that already happened (e.g., `CreatedAt`, a machine stop) as `timestamp with time zone`. Use a `DateTime` with `DateTimeKind.Utc`.
    *   **Future Local Times:** Store a planned local time (e.g., a shift start, a maintenance slot) as `timestamp without time zone` plus the IANA time zone ID in a `text` column. Use a `DateTime` with `DateTimeKind.Unspecified`. The local time and the zone are the source of truth.
    *   **Derived UTC Column (Optional Solution):** Do not add a UTC copy by default. Query the local time and the zone. Add a UTC copy only when a use case must filter, sort, or index future local times across time zones. Then store it as `timestamp with time zone` next to the local time (e.g., `StartsAtLocal` and `StartsAtUtc`). Compute it with `TimeZoneInfo.ConvertTimeToUtc` in the entity method that sets the local time or the zone. The UTC copy does not change when time zone rules change, so read the local time and the zone for display.
    *   **Dates and Times Only:** Use `DateOnly` (`date`) or `TimeOnly` (`time`) only when the use case has no time or no date part.
    *   **Time Zone IDs:** Store every time zone as an IANA ID (e.g., `Europe/Kyiv`). Validate it with `TimeZoneInfo.TryFindSystemTimeZoneById`.
*   **Auditing:** `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy` MUST be populated using EF Core's `SaveChangesInterceptor` (`AuditableEntityInterceptor`), which targets entities based on `EntityState.Added` and `EntityState.Modified`.
*   **DbContext Implementation:** Name the concrete context after its service (e.g., `CoreDbContext`). Derive it directly from `DbContext`. Do not add a shared base context. Implement the Application interface of the service (e.g., `ICoreDbContext`), `IOutboxDbContext`, and `IInboxDbContext`.
    *   **Messaging Tables:** In `OnModelCreating`, call `ApplyOutboxConfiguration()` and `ApplyInboxConfiguration()` before `ToSnakeCaseConventions()`. The pending-message index filters use snake case column names.
    *   **Registration:** Register the context only with `AddMessagingDbContext<TInterface, TContext>()`. It maps the Application interface, `IOutboxDbContext`, `IInboxDbContext`, `IOutbox`, and `IInbox` to one scoped instance. A separate registration creates a second instance and splits the transaction.
    *   **One Scope:** Never create a DI scope or use `IDbContextFactory` inside a use case. A second scope gives a second context, so its changes leave the business transaction.
*   **Entity Configurations:** Every entity must have its own configuration file implementing `IEntityTypeConfiguration<TEntity>`.
*   **No Inline Entity Configuration:** Do not configure entity properties or keys inline inside `DbContext` or `OnModelCreating`.
*   **DbContext Scope:** Inline configuration inside `OnModelCreating` is permitted only for model-wide settings unrelated to specific entities (e.g., PostgreSQL extensions, snake-case naming).
*   **Reusable Configuration Extensions:** Use shared `EntityTypeBuilder` extension methods (`ConfigureEntity()`, `ConfigureAuditable()`) for `IEntity` and `IAuditable` interfaces to apply baseline configurations.
*   **Configuration Location:** Place entity configurations inside their feature folder under `Persistence/` (e.g., `/Machines/Persistence/MachineConfiguration.cs`).
*   **Check Constraints:** Mirror each stateless validator rule (not blank, length, range, not empty GUID, allowed enumeration values) with a database check constraint. Use the builders in `HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints` (`HasNotBlankCheck`, `HasLengthCheck`, `HasRangeCheck`, `HasNotEmptyGuidCheck`, `HasEnumerationCheck`) inside the entity configuration. Do not mirror stateful rules. Register every enumeration with `HaveEnumerationConversion<T>()` in `ConfigureConventions`, or EF Core treats it as an entity and fails. Then add `HasEnumerationCheck()` in the entity configuration. Add a migration with each new constraint or enumeration member change.
*   **Outbox and Inbox:** Use the transactional outbox and inbox patterns when a use case sends or receives messages across services. Do not add them to a use case that stays inside one service.
    *   **Outbox:** Save each outgoing message as an outbox row in the same transaction as the business change. Never publish to the broker directly from a handler. A background outbox processor reads the pending rows, publishes them, and sets `ProcessedOn`.
    *   **Inbox:** Save each received message as an inbox row before you handle it. Use the message ID as a unique key, so a duplicate delivery does not create a second row. A background inbox processor handles the pending rows and sets `ProcessedOn` in the same transaction as the business change. This makes every consumer idempotent.
    *   **Filtered Index:** Add a filtered index on the outbox table and on the inbox table with the filter `processed_on IS NULL`. The processors then scan only the pending rows, and the index stays small.
