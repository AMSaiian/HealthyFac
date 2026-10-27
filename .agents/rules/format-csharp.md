---
trigger: glob
globs: "**/*.cs"
description: "C# Formatting rules extracted from .editorconfig"
---
# C# Code Formatting Rules (.editorconfig)
*   **Indentation & Lines:** 4 spaces per indent. Max line length is 120 characters.
*   **Implicit Typing (`var`):** Always use `var` (built-in types, apparent types, and everywhere else).
*   **Type Aliases:** Use predefined C# type keywords (`string`, `int`, `bool`) instead of BCL types (`String`, `Int32`).
*   **No `this.` Prefix:** Do not qualify fields, properties, or methods with `this.`.
*   **Mandatory Braces:** Always use curly braces `{ }` for `if`, `else`, `for`, `foreach`, and `while` statements, even for single lines.
*   **Expression Bodies:** Use expression-bodied members (`=>`) for methods and operators when the logic is a single expression.
*   **Trailing Commas:** Always include trailing commas in multiline lists, enums, and object initializers.
*   **Wrapping & Chopping:** If method parameters, chained calls (LINQ), or binary expressions exceed 120 chars, chop them so **every** item gets its own line. Insert newlines after opening `(` and before closing `)`.
*   **Binary Operators:** Break the line *before* the operator so the operator starts the new line.
*   **Alignment:** Do **NOT** use extra spaces to align multiline binary expressions, statement conditions, or chained calls. Rely entirely on standard indentation levels.
*   **Attributes:** Always place attributes on a separate line above the target. Do not place them on the same line.
*   **Blank Lines:** Max 1 consecutive blank line inside code blocks.
*   **Primary Constructors:** Wrap primary constructor parameters to separate lines if there are more than 4 parameters.
*   **Access Modifiers:** Always explicitly declare access modifiers (e.g., private, internal, public) for classes, methods, and fields. Do not omit them and rely on C# defaults.
*   **Private Field Names:** Name private instance fields in camelCase with the `_` prefix (e.g., `_dbContext`, `_isStarted`).
*   **Method Order:** Order methods by access modifier: public first, then internal, protected, and private. Never place a private method between two public methods. Within one modifier group, put a caller above the helpers it calls. The file then reads from the overview to the details. Fields, constructors, and properties stay above all methods.
