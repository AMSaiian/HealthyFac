---
trigger: glob
globs: "**/*.Web/**"
description: "Rules for the Web layer."
---
# Web Layer Rules
*   **API Paradigm:** No MVC Controllers. Use Minimal APIs exclusively.
*   **Handler Injection:** Inject the specific generic `IRequestHandler<TRequest, TResponse>` directly into the Minimal API delegate. Do not inject a mediator or dispatcher.
*   **Logging:** Ensure Serilog request logging is utilized.
*   **Endpoint File Separation:** Never define Request or Response contracts inside an Endpoint file. Keep Endpoints, Requests, and Responses in separate files within their use-case sub-folder (e.g., `/Machines/CreateMachine/`).
*   **Configuration & FluentValidation:** Strongly-typed configuration options (classes bound to configuration sections via the Options pattern) must be validated using FluentValidation (`AbstractValidator<TOptions>`). Enforce validation at application startup with `.ValidateOnStart()` (e.g., via `AddValidatedOptions<TOptions, TValidator>()`) so misconfigured settings fail fast before requests are served. Never rely on unvalidated configurations or ad-hoc manual checks scattered across services.
*   **Extension Method Prefixes:** Use correct conventional `Add...`, `Use...` prefixes for methods that involve application configuration and setup:
    *   Use `Add...` for extension methods that register services or configure options on `IServiceCollection` or `OptionsBuilder<TOptions>` (e.g., `AddIdEncryption`, `AddSharedExceptionHandling`, `AddValidatedOptions`).
    *   Use `Use...` for extension methods that configure middleware or the HTTP request pipeline on `IApplicationBuilder` or `WebApplication` (e.g., `UseSharedExceptionHandling`).
    *   Use `Map...` for extension methods that map endpoints on `IEndpointRouteBuilder` (e.g., `MapMachineEndpoints`).
*   **Service Registration Grouping:** Group related service registrations under dedicated extension classes (`ConfigureServices.cs`). Do not scatter registrations in `Program.cs`. Encapsulate related dependencies into private extension methods (e.g., `AddCustomServices`, `AddApiRouting`). Expose a high-level registration method (e.g., `AddApiServices`) for the Web project.
*   **Concern Grouping:** Group registrations that serve one concern into a private `Add...` method named after the concern, such as `AddApiLogging`, `AddApiSerialization`, `AddApiSecurity`, or `AddApiObservability`. Create a group only when the concern needs three or more calls. Leave one or two calls in the caller. `AddApiServices` calls the groups and the loose calls in order. Apply the same rule to `UseApiPipeline` (group by pipeline stage) and `MapApiEndpoints` (group by feature). Do not split by line count.
*   **Pipeline and Route Grouping:** Group middleware pipeline configuration into dedicated `Use...` extension methods (e.g., `UseApiPipeline`). Group API route mappings into dedicated `Map...` extension methods (e.g., `MapApiEndpoints`).
*   **Database Initialization and Seeding Hooks:** Provide an initialization extension method (e.g., `InitializeDatabaseAsync`) on `IServiceProvider` in the Infrastructure layer, so every host shape can call it. `Program.cs` calls it after the host builds. Resolve `IApplicationDbInitializer` within a created service scope. Execute database migrations using `ApplyDatabaseStructureAsync`. The initializer migrates only when the environment is Development. Apply migrations in every other environment through a deployment step. Execute required data seeding (e.g., measurement units) using `SeedRequiredDataAsync`. Execute optional data seeding (e.g., Bogus mock data) using `SeedOptionalDataAsync` when enabled in configuration.
