---
trigger: glob
globs: "**/*.Domain/**"
description: "Rules for the Domain layer."
---
# Domain Layer Rules
*   **Base Interface:** Every entity implements `IEntity` or `IEntity<TId>` (`HealthyFac.Shared.Domain.Entities`).
*   **Auditing:** Implement `IAuditable` on every entity that a user can create or update; system-only data (e.g., seeded reference data) does not need it. Never set `CreatedAt`, `CreatedBy`, `UpdatedAt`, or `UpdatedBy` in a constructor: the `AuditableEntityInterceptor` sets them.
*   **Soft Delete:** Every entity implements `ISoftDeletable`, and its configuration calls `ConfigureSoftDeletable()`. A removal goes through `Remove`; the `SoftDeleteInterceptor` turns it into an update. Every unique index on the entity filters out deleted rows (`SoftDeleteConstants.NotDeletedFilter`). A query that must see deleted rows calls `IgnoreQueryFilters()`. The architecture tests and `SoftDeleteModelTests` check it.
*   **Encapsulation:** Use `private set` or `init` for entity properties.
*   **Rich Domain Models:** Put each state change that must keep an invariant into an entity method (e.g., `machine.StartMaintenance()`), mainly on aggregate roots. Keep an entity without invariants as plain properties.
*   **Entity Naming:** Name entities with singular nouns (`Machine`), so they do not collide with the plural feature namespaces.
*   **ID Generation:** Initialize `Id` with `Guid.CreateVersion7()` (`public Guid Id { get; private set; } = Guid.CreateVersion7();`), so the primary key index stays append-only. The column has no database default (`ConfigureEntity()` maps `ValueGeneratedNever()`). The ID exists before the first save, so one `SaveChangesAsync` writes an entity and every row that references it. Never open a transaction only to read a generated ID.
    *   **Seed Exception:** Only the development seed may use a fixed ID, read from validated options, when an external system must know it first (e.g., the test company ID, which is also the Keycloak organization alias). Never hardcode it.
*   **Navigations:** Add a navigation, or its inverse, only when an entity method, a query, or a use case walks it. Otherwise keep a plain foreign key property (e.g., `Machine.CompanyId`).
    *   **Nullability:** Declare the reference navigation of a required relationship as non-nullable with `= null!` and a `private set` (`public Company Company { get; private set; } = null!;`). Make it nullable only when the business relationship is optional. EF Core fills it only when a query loads it (e.g., `Include`). This is the only allowed use of `= null!`.
    *   **Collections:** Expose a collection as `IReadOnlyCollection<T>` over a private `List<T>` backing field (`_departments`), and change it only through entity methods. EF Core loads and tracks the backing field.
    *   **Reference Cycles:** Navigations on both sides create cycles. Never serialize an entity; map it to a response type. Compare such graphs in tests with `IgnoringCyclicReferences()`.
