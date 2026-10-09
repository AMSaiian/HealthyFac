---
trigger: glob
globs: "**/*.{ts,js}"
description: "TypeScript and JavaScript formatting rules from .editorconfig"
---
# TypeScript / JavaScript Code Formatting Rules (.editorconfig)
*   **Field Naming:** Prefix all private/internal fields with an underscore (field_prefix = _).
*   **Indentation:** 2 spaces per indent.
*   **Quotes:** Use **single quotes** (`use_double_quotes = false`).
*   **Semicolons:** Always enforce semicolons (`force_semicolon_style = true`).
*   **Braces:** Use "1TBS" style (braces at the end of the line, `end_of_line`). Always use braces for `if`, `for`, `while`, `do`.
*   **Type Casting:** Prefer `as Type` instead of `<Type>` (`prefer_as_type_cast = true`).
*   **Alignment:** Do **not** space-align multiline array initializers, object properties, binary operations, or parameters (`align_* = false`).
*   **Imports:** Sort members within imports (`import_sort_members = true`). Blacklist raw `rxjs/Rx` imports.
*   **Imports (Path Aliases):** Always use configured path aliases (e.g., `@app/...`, `@core/...`) for imports. Do NOT use deeply nested relative paths (e.g., `../../../utils`).
*   **No Magic Values:** Never hardcode literal numbers, strings, URLs, or config keys directly in code. Extract them into named `const` exports in a dedicated file (e.g., `constants.ts`, `api-routes.ts`) or use a string enum. The only acceptable inline literals are `0`, `1`, `-1`, `true`, `false`, `null`, `undefined`, and empty string `''`.
*   **Enums:** Always use **string enums** for domain constants. Do NOT use numeric enums or `const enum`. String enums provide runtime-iterable objects (`Object.values()`), meaningful values that match API contracts, and full type safety.
*   **Enum Literal Casing:** Name each enum member in PascalCase. Write each literal value in camelCase, so it matches the casing of the JSON properties around it (e.g., `enum MachineState { Operational = 'operational', InMaintenance = 'inMaintenance' }`). The backend emits the same camelCase literals, so no translation layer is required.
