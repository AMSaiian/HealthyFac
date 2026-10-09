---
trigger: glob
globs: "**/*.Application/**"
description: "Rules for the Application layer."
---
# Application Layer Rules
*   **CQRS Pattern:** Do not use MediatR. Implement Use Cases via custom CQRS interfaces in `HealthyFac.Shared.Application.UseCases` (`IRequest`, `IRequest<TResponse>`, `IRequestHandler`).
*   **Validation Boundaries:** Use `FluentValidation` strictly for **simple, stateless input validation** (e.g., string lengths, null checks, safe ranges). **Do NOT** use FluentValidation for stateful business rules (e.g., querying the DbContext for uniqueness). Stateful business validation must remain in the application code inside the `IRequestHandler`.
*   **Decorator Execution:** Do not call `.ValidateAsync()` manually inside the handler; stateless input validation is executed automatically by the `ValidationRequestHandlerDecorator`.
*   **Time:** Always use `IDateProvider.GetUtc()` (from `Shared.Application.Interfaces`) for time. Never use `DateTime.UtcNow` directly in application logic.
*   **Cross-Cutting Concerns:** Handled strictly via the Decorator Pattern using Scrutor (e.g., `ValidationRequestHandlerDecorator`, `LoggingRequestHandlerDecorator`).
*   **Use Case Co-location:** It is permissible and encouraged to place an `IRequest` (and any use-case-specific response record) in the same file alongside its `IRequestHandler`.

*   **DbContext Access:** Declare every `DbSet<TEntity>` on the abstract `CoreDbContext` in the Application layer, and inject `CoreDbContext` into handlers. Never reference the Infrastructure layer from Application. Never add a DbContext interface. The Infrastructure layer derives the concrete context, adds the provider and the entity configurations, and registers the derived context as `CoreDbContext`.
