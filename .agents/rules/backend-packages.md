---
trigger: glob
globs: ["**/*.csproj", "**/*.props"]
description: "Rules for project files, package versions, and project references."
---
# Backend Project and Package Rules
*   **Centralized Package Management (CPM):** Manage all NuGet package versions centrally in `Directory.Packages.props` using `<PackageVersion Include="..." Version="..." />`. Individual `.csproj` files must define `<PackageReference Include="..." />` without an inline `Version` attribute.
*   **Version Overrides:** A `PackageVersion` that overrides a transitive version, or that no code uses directly, has an XML comment directly above it. The comment states the reason (e.g., the advisory IDs of a vulnerability) and the condition for its removal (e.g., "Remove this pin when MailKit depends on a patched version."). Reference the package only in the project that brings the transitive dependency.
*   **Shared Project:** Microservices must reference their corresponding HealthyFac.Shared.* layer (e.g., Core.Domain references Shared.Domain).
