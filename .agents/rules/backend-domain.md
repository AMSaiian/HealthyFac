---
trigger: glob
globs: "**/*.Domain/**"
description: "Rules for the Domain layer."
---
# Domain Layer Rules
*   **Entities:** Prefer Interfaces over Abstract Classes. Do not use abstract base entity classes.
*   **Base Interface:** Entities must implement `IEntity` or `IEntity<TId>` from `HealthyFac.Shared.Domain.Interfaces`.
*   **Auditing:** Entities requiring auditing must implement `IAuditable`.
*   **Constructors:** Do not populate Audit fields (`CreatedAt`, `UpdatedAt`) inside the entity constructor. These are managed externally by Interceptors.
*   **Encapsulation:** Use `private set` or `init` for entity properties to ensure encapsulation.
*   **Rich Domain Models (Pragmatic DDD):** Use rich domain models where possible to encapsulate behavior, but implement them pragmatically (avoiding "nerdy fanaticism"). 
*   **Aggregate Roots:** The primary target for rich domain model patterns should be your Aggregate Roots. Minor entities or simple data structures can remain straightforward.



*   **Entity Naming:** Always use singular nouns for entity classes and domain models (e.g., `Machine`, `Order`). Never use plural nouns for entity classes. This convention avoids collisions with plural feature namespaces.
*   **ID Generation:** Never generate an identifier in application code. The database generates it. The shared `ConfigureEntity()` maps `IEntity.Id` to the native PostgreSQL 18 `uuidv7()` default, which yields time-ordered values optimised for index locality. Leave `Id` at `Guid.Empty` in the entity. Treat `Guid.Empty` as the marker for an unsaved entity. Read the generated value after `SaveChangesAsync`.
