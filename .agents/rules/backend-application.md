---
trigger: glob
globs: "**/*.Application/**"
description: "Rules for the Application layer."
---
# Application Layer Rules
*   **CQRS Pattern:** Do not use MediatR. Implement Use Cases via custom CQRS interfaces in `HealthyFac.Shared.Application.UseCases` (`IRequest`, `IRequest<TResponse>`, `IRequestHandler`).
*   **Validation Boundaries:** Use `FluentValidation` strictly for **simple, stateless input validation** (e.g., string lengths, null checks, safe ranges). **Do NOT** use FluentValidation for stateful business rules (e.g., querying the DbContext for uniqueness). Stateful business validation must remain in the application code inside the `IRequestHandler`.
*   **Decorator Execution:** Do not call `.ValidateAsync()` manually inside the handler; stateless input validation is executed automatically by the `ValidationRequestHandlerDecorator`.
*   **Time:** Always use `IDateProvider.GetUtc()` (from `Shared.Application.Time`) for time. Never use `DateTime.UtcNow` directly in application logic.
*   **Cross-Cutting Concerns:** Implement a concern as a request handler decorator, registered with Scrutor, when every use case needs it in the same form (e.g., `ValidationRequestHandlerDecorator`, `LoggingRequestHandlerDecorator`). Keep logic that varies between use cases in the handler.
*   **Use Case Co-location:** Place the `IRequest`, its use-case-specific response, and its `IRequestHandler` in one use-case file.
*   **DbContext Access:** Declare every `DbSet<TEntity>` on the abstract `CoreDbContext` in the Application layer, and inject `CoreDbContext` into handlers. Never reference the Infrastructure layer from Application. Never add a DbContext interface. The Infrastructure layer derives the concrete context, adds the provider and the entity configurations, and registers the derived context as `CoreDbContext`.
