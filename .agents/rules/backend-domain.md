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
*   **Navigations:** Add a navigation only when code uses it: an entity method, a query, or a use case. Do not add navigations or inverse navigations in advance. A relationship without a navigation stays a plain foreign key property (e.g., `Machine.CompanyId`). Add the inverse side when a use case must walk the relationship in that direction.
    *   **Nullability by Business Model:** Show the business rule in the type. Declare the reference navigation of a required relationship as non-nullable with `= null!` and a `private set` (e.g., `public Company Company { get; private set; } = null!;`). Make it nullable only when the business relationship is optional. EF Core fills a navigation only when a query loads it, so load it (e.g., with `Include`) before you read it. This is the only allowed use of `= null!`.
    *   **Collections:** Expose a collection navigation as `IReadOnlyCollection<T>` over a private `List<T>` backing field (e.g., `_departments`). Change it only through entity methods, so outside code cannot skip their invariants. EF Core loads and tracks the backing field.
    *   **Reference Cycles:** A relationship with navigations on both sides creates reference cycles. Never serialize an entity directly; map it to a response type. Compare such entity graphs in tests with `IgnoringCyclicReferences()`.
