---
trigger: glob
globs: ["backend/tests/**", "**/*Tests/**", "**/*Test/**"]
description: "Rules for test projects, unit tests, and integration tests."
---
# Testing Rules
*   **Testing Stack:** Use `xUnit` as the test runner, `Testcontainers` for integration environments (PostgreSQL, RabbitMQ, Keycloak), and `FluentAssertions` (v7.x) for all assertions.
*   **FluentAssertions v7:** Always use `FluentAssertions` v7 (Apache 2.0). Never use `Assert.*` methods from xUnit. Never upgrade to FluentAssertions v8+ due to commercial licensing restrictions.
*   **Theory Data (No InlineData):** Never use `[InlineData]`. Always use `[MemberData(nameof(...))]` with strongly-typed static member properties/methods, or dedicated `TheoryData<...>` / `ClassData` providers.
*   **Test Data Separation (Data/ Subfolder):** Never inline test data or `TheoryData` properties inside the test class. Always extract test data into a dedicated file located in a `Data/` subfolder adjacent to the test class (e.g., `Feature/Data/FeatureTestData.cs`). Reference it via `[MemberData(nameof(FeatureTestData.DataProperty), MemberType = typeof(FeatureTestData))]` or `[ClassData]`.
*   **Naming Convention:** Test methods must follow `UnitOfWork_StateUnderTest_ExpectedBehavior` (e.g., `Encrypt_WithSameGuidAndKey_ProducesExactSameCiphertext`).
*   **Structure:** Follow Arrange-Act-Assert flow without writing inline `// Arrange`, `// Act`, `// Assert` comments.
*   **Modifiers:** Declare a test class as `public sealed class`. Use `public class` only when another test class derives from it. Declare test methods as `public void` or `public async Task`.
*   **Global Usings:** Test projects must declare global usings for `Xunit` and `FluentAssertions` via `<Using Include="Xunit" />` and `<Using Include="FluentAssertions" />` in their `.csproj` file. Individual test files must not declare local `using Xunit;` or `using FluentAssertions;`.
*   **No Tests for Boilerplate:** Never write tests for boilerplate or trivial code. Do not test auto-properties, plain DTO or record construction, object initializers, constructor assignment, pass-through methods that only delegate to a dependency, EF Core entity configurations, or service registration. Do not test the framework or third-party library behavior. Test real logic instead: business rules, validation outcomes, branching, calculations, error paths, and integration boundaries. Apply one check before you write a test: can this code fail for a reason other than a typo? Write the test only when the answer is yes.
