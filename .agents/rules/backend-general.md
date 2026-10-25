---
trigger: glob
globs: "**/*.cs"
description: "General backend C# conventions for all layers."
---
# Backend General Rules
*   **Variable Inlining & Declaration:** Inline every expression by default. Declare a local variable only in these situations:
    *   The value cannot be resolved without an intermediate variable.
    *   The variable captures the result of an `await` expression.
    *   The variable stores the result of a LINQ query or chained operation.
    *   The variable names a condition or an intermediate result with a business term (e.g., `isMaintenanceDue`, `remainingCapacity`). When no business term fits, inline the expression.
*   **Awaited Results:** Store an awaited result in a variable before you use it. Never wrap an `await` in parentheses to read a member or chain a call (e.g., `(await LoadAsync()).Name` or `(await ReadAsync()).Where(...)`). Never write an expression-bodied async method whose body uses an awaited result; write a block body instead. Name an awaited condition (e.g., `var isEmailEmployed = await ...AnyAsync(...); if (isEmailEmployed)`). In tests, chain FluentAssertions on the awaited assertion task (`await act.Should().ThrowAsync<T>().WithMessage(...)`).
*   **Boolean Naming:** Prefix every boolean variable, field, property, and parameter with `is`, `has`, or `can`, as the context requires (e.g., `isActive`, `hasOpenTickets`, `canSchedule`).
*   **Documentation (All or Nothing):** Decide for each unit of code (type, method, property, field, or constant) on its own. Document a unit fully or not at all.
    *   **Trivial unit:** Write no comment. A unit is trivial when its name, signature, and types explain what it does.
    *   **Non-trivial unit:** The code cannot show the reason: a constraint, a unit of measure, a side effect, an ordering requirement, a deliberate workaround, or complex logic such as a cryptographic algorithm.
    *   **Full documentation:** Write a `<summary>`. Add a `<typeparam>` for every type parameter, a `<param>` for every parameter, and a `<returns>` for every non-void result. Put the reason in `<summary>` or `<remarks>`. Never restate the member name or the code.
    *   **References:** Reference code symbols with `<see cref="..."/>` and parameters with `<paramref name="..."/>`.
    *   **No Inline Comments:** Do not write `//` comments that explain what code does, unless the user asks.
*   **Exception Documentation:** Document exceptions that a method throws directly by itself. Do not document exceptions that a method merely proxies or bubbles up from dependencies. List the thrown exception in the method description using `<see cref="..."/>`. Add an `<exception cref="...">` tag explaining the exact condition that triggers the exception.
*   **Primary Constructors:** Use C# 12 Primary Constructors **only** for injecting dependencies into classes (the parameters act as your private fields). **Do NOT** use primary constructors for record types (e.g., DTOs, Commands, Events). For records, declare explicit properties with init setters (e.g., public string Name { get; init; }).
*   **Layer Boundaries:** Keep strict Clean Architecture boundaries (Web -> Infrastructure -> Application -> Domain).
*   **Screaming Architecture:** Name folders after what the code does, not after its technical type.
    *   **Shared Projects:** Group code by intent (e.g., `/IdEncryption/`, `/ErrorHandling/`, `/Logging/`).
    *   **Service Projects:** Group code by business feature, then by use case (e.g., `/Machines/CreateMachine/`). The Web layer groups by feature only (`backend-web.md`, rule **Endpoint Grouping**). Keep feature-wide definitions (e.g., `MachineErrors`, `MachineValidationConstants`) at the feature folder root.
    *   **Plural Root Names:** Use plural nouns for root folders and their namespaces (e.g., `/Machines/`, `/Orders/`). A singular name collides with the singular entity class (CS0118) and forces fully qualified identifiers.
    *   **Technical Sub-folders:** When several files in a business folder serve the same technical matter, move them into a sub-folder named after that matter (e.g., `/Machines/Persistence/`). Never create root folders for technical concerns (e.g., `/Services/`, `/Models/`, `/Handlers/`).
*   **Interfaces vs. Base Classes:**
    *   Use an interface to label a type, to define a contract, or to limit the types that an API accepts (e.g., `IEntity`, `IAuditable`).
    *   Use a base class only to lock behavior that every derived class must follow, such as a fixed method structure or a required step.
    *   Do not use a base class only to reuse logic. Reuse logic through composition: inject or call a separate type.
*   **Object Initialization:** For simple data assignment without logic, rely on init properties and object initializers (e.g., new Dto { Prop = value }). However, if object creation involves **any** kind of logic (e.g., validation, derived fields), you must use an explicit constructor.
*   **No Magic Values:** In web, infrastructure, and business code, extract a literal into a named constant when a change to its value would require edits in more than one place. This covers numbers, strings, URLs, configuration keys, route templates, policy names, and validation limits. Put the constants in a dedicated `static class` (e.g., `ValidationConstants`, `PolicyNames`, `CacheKeys`).
    *   **Always Inline:** `0`, `1`, `-1`, `true`, `false`, `null`, `string.Empty`, and `Guid.Empty`.
    *   **Log Messages:** Write log message templates inline, so a project search finds them.
*   **Error Definitions:** Never inline error messages or instantiate ad-hoc exceptions with raw strings directly in logic. Define errors in dedicated `static class` definitions (e.g., `MachineErrors`, `AuthErrors`) scoped to their feature, exposing static error instances or factory methods, and reference them.
*   **File Separation:** The Web layer defines no request or response contracts, except a body record for server-owned values, which shares its file with its `ToRequest` extension class; endpoints use the Application types (`backend-web.md`, rule **API Contracts**). In the Application layer, place an `IRequest`, its response, and its `IRequestHandler` in one use-case file. Classes/types may otherwise be inlined ONLY if they are utility/helper types used strictly within that file.
*   **Required Modifier:** Use `required` with `init` instead of dummy default values (e.g., avoid `= string.Empty;` or `= default!;`) for non-nullable properties. This guarantees compile-time initialization safety without silencing nullable analysis with placeholders.
    *   **Where:** DTOs, Requests, Responses, and entities with no invariants.
    *   **Entities with Invariants:** Set the properties in a constructor and use `private set`. C# rejects `required` on a property with a `private set` (CS9032).
    *   **Navigation Exception:** A required reference navigation uses `= null!`, because EF Core sets it and it is not part of object creation. Read the **Navigations** rule in `backend-domain.md`.
*   **Extension Method Prefixes:** Use the conventional prefixes for extension methods that configure and set up the application:
    *   Use `Add...` for extension methods that register services or configure options on `IServiceCollection` or `OptionsBuilder<TOptions>` (e.g., `AddIdEncryption`, `AddSharedExceptionHandling`, `AddValidatedOptions`).
    *   Use `Use...` for extension methods that configure middleware or the HTTP request pipeline on `IApplicationBuilder` or `WebApplication` (e.g., `UseSharedExceptionHandling`).
    *   Use `Map...` for extension methods that map endpoints on `IEndpointRouteBuilder` (e.g., `MapMachineEndpoints`).
*   **Options Section Names:** Every strongly-typed options class bound through the Options pattern must declare its configuration section name as a `public const string SectionName` on that options class. Never place an options section name in a separate constants class, and never hardcode it at the registration call site. Do not store composite paths (e.g. `"IdEncryption:Keys"`) as extra constants. Compose them from `SectionName` where a full path is required.
*   **Constant Grouping:** Group related constants into nested `static class` blocks inside the owning constants class once that class holds five or more members or covers more than one concern. Name each nested class after the concern it serves, not after the member type (e.g. `ProblemDetailsConstants.NotFound.Title`, `HealthCheckConstants.Routes.Liveness`, `DatabaseConstants.Retry.MaxCount`). Keep a constants class flat only while it stays small and single-purpose. Never introduce a nested class that holds a single member.
*   **No Suppressions:** Do not suppress analyzer warnings unless the user asks. This covers `[SuppressMessage]`, `#pragma warning disable`, `GlobalSuppressions.cs`, and `.editorconfig` severity overrides. Report the warning and the reason it may be a false positive. Let the user decide.
