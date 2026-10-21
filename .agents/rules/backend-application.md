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
*   **DbContext Access:** Declare every `DbSet<TEntity>` on the `ICoreDbContext` interface in the Application layer. Inject `ICoreDbContext` into handlers. Never inject the concrete `CoreDbContext` or a plain `DbContext`. Never reference the Infrastructure layer from Application.
    *   **Allowed Abstraction Leak:** `ICoreDbContext` exposes EF Core types such as `DbSet<TEntity>`. It may mirror a `DbContext` member signature that a handler needs (e.g., `SaveChangesAsync`). Add a member only when a use case calls it. Do not hide EF Core behind repositories.
    *   **Business Sets Only:** `ICoreDbContext` declares business sets only. Do not add the outbox or inbox sets to it.
*   **Cross-Service Messages:** When a handler sends a message to another service, inject `IOutbox` and call `Add`. Then call `SaveChangesAsync` on `ICoreDbContext` once, so the outbox row and the business change commit in one transaction. Do not open an explicit transaction for this. Never publish to the broker from a handler. Read the **Outbox and Inbox** rule in `backend-infrastructure.md`.
