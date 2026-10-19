---
trigger: glob
globs: "**/*.Domain/**"
description: "Rules for the Domain layer."
---
# Domain Layer Rules
*   **Base Interface:** Entities must implement `IEntity` or `IEntity<TId>` from `HealthyFac.Shared.Domain.Entities`.
*   **Auditing:** Implement `IAuditable` on every entity that a user can create or update. An entity that only the system writes (e.g., seeded reference data) does not need it.
*   **Constructors:** Do not set the `IAuditable` fields (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) in the entity constructor. The `AuditableEntityInterceptor` sets them.
*   **Encapsulation:** Use `private set` or `init` for entity properties to ensure encapsulation.
*   **Rich Domain Models:** Put each state change that must keep an invariant into an entity method (e.g., `machine.StartMaintenance()`). Keep an entity with no invariants as plain properties.
*   **Aggregate Roots:** The primary target for rich domain model patterns should be your Aggregate Roots. Minor entities or simple data structures can remain straightforward.
*   **Entity Naming:** Always use singular nouns for entity classes and domain models (e.g., `Machine`, `Order`). Never use plural nouns for entity classes. This convention avoids collisions with plural feature namespaces.
*   **ID Generation:** Never generate an identifier in application code. The database generates it. The shared `ConfigureEntity()` maps `IEntity.Id` to the native PostgreSQL 18 `uuidv7()` default, which yields time-ordered values optimised for index locality. Leave `Id` at `Guid.Empty` in the entity. Treat `Guid.Empty` as the marker for an unsaved entity. Read the generated value after `SaveChangesAsync`.
