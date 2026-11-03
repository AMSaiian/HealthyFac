---
trigger: glob
globs: "**/*.{ts,tsx,js,jsx}"
description: "TypeScript and JavaScript formatting rules from .editorconfig"
---
# TypeScript / JavaScript Code Formatting Rules
*   **Tools:** Prettier and ESLint enforce indentation, quotes, semicolons, braces, imports, field prefix, and enums. The Stop hook runs them.
*   **No Magic Values:** Extract a literal into a named constant when a change to its value would require edits in more than one place. This covers numbers, strings, URLs, config keys, and API routes. Put the constants in named `const` exports in a dedicated file (e.g., `constants.ts`, `api-routes.ts`) or use a string enum.
    *   **Always Inline:** `0`, `1`, `-1`, `true`, `false`, `null`, `undefined`, and `''`.
    *   **Log Messages:** Write log messages inline, so a project search finds them.
    *   **Library Constants:** First use a constant of Angular or a referenced package with the same meaning (e.g., `HttpStatusCode` from `@angular/common/http`). Add your own only when none fits. ESLint blocks HTTP status literals in comparisons.
*   **Enum Literal Casing:** Name each enum member in PascalCase. Write each literal value in camelCase, so it matches the casing of the JSON properties around it (e.g., `enum MachineState { Operational = 'operational', InMaintenance = 'inMaintenance' }`). The backend emits the same camelCase literals, so no translation layer is required.
