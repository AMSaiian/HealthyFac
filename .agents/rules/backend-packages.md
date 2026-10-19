---
trigger: glob
globs: ["**/*.csproj", "**/*.props"]
description: "Rules for project files, package versions, and project references."
---
# Backend Project and Package Rules
*   **Centralized Package Management (CPM):** Manage all NuGet package versions centrally in `Directory.Packages.props` using `<PackageVersion Include="..." Version="..." />`. Individual `.csproj` files must define `<PackageReference Include="..." />` without an inline `Version` attribute.
*   **Shared Project:** Microservices must reference their corresponding HealthyFac.Shared.* layer (e.g., Core.Domain references Shared.Domain).
