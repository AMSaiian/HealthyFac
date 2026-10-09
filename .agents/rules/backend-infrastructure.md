---
trigger: glob
globs: "**/*.Infrastructure/**"
description: "Rules for the Infrastructure layer."
---
# Infrastructure Layer Rules
*   **ORM:** Use Entity Framework Core.
*   **Datetimes:** Store all dates in the database as UTC without timezone offsets.
*   **Auditing:** `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy` MUST be populated using EF Core's `SaveChangesInterceptor` (`AuditableEntityInterceptor`), which targets entities based on `EntityState.Added` and `EntityState.Modified`.
*   **Entity Configurations:** Every entity must have its own configuration file implementing `IEntityTypeConfiguration<TEntity>`.
*   **No Inline Entity Configuration:** Do not configure entity properties or keys inline inside `DbContext` or `OnModelCreating`.
*   **DbContext Scope:** Inline configuration inside `OnModelCreating` is permitted only for model-wide settings unrelated to specific entities (e.g., PostgreSQL extensions, snake-case naming).
*   **Reusable Configuration Extensions:** Use shared `EntityTypeBuilder` extension methods (`ConfigureEntity()`, `ConfigureAuditable()`) for `IEntity` and `IAuditable` interfaces to apply baseline configurations.
*   **Configuration Location:** Place entity configurations inside their feature folder under `Persistence/` (e.g., `/Machines/Persistence/MachineConfiguration.cs`).
*   **Check Constraints:** Mirror each stateless validator rule (not blank, length, range, not empty GUID, allowed enumeration values) with a database check constraint. Use the builders in `HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints` (`HasNotBlankCheck`, `HasLengthCheck`, `HasRangeCheck`, `HasNotEmptyGuidCheck`, `HasEnumerationCheck`) inside the entity configuration. Do not mirror stateful rules. Register every enumeration with `HaveEnumerationConversion<T>()` in `ConfigureConventions`, or EF Core treats it as an entity and fails. Then add `HasEnumerationCheck()` in the entity configuration. Add a migration with each new constraint or enumeration member change.
