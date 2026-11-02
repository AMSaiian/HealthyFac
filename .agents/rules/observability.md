---
trigger: glob
globs: ["**/Observability/**", "**/Tracing/**", "**/Metrics/**", "**/Logging/**"]
description: "Rules for telemetry resource attributes, span names, metric names, and tags."
---
# Observability Rules
*   **Resource Attributes:** Set them only in `TelemetryResource` (Shared.Web). Traces, metrics, and logs use it. The deployment or the collector sets platform attributes (`k8s.*`, `host.*`). `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` override the configured values.
*   **Span Names:** Name an internal span `<Area>.<Action>` in PascalCase (`Razor.Render`, `UseCase.Validate`). The use case span keeps the request type name. Library spans keep their names (HTTP, messaging `{operation} {destination}`).
*   **Keys and Names:** Write attribute and tag keys in lower case, with dot namespaces and snake_case words (`healthyfac.use_case`). Name each source and meter after its .NET namespace (`HealthyFac.Messaging`). Define every name in `ObservabilityConstants`.
*   **Metric Names:** Write them in lower case with dots (`healthyfac.messaging.handled`). Put the unit in the instrument (`s`, `{message}`), never in the name.
*   **Cardinality:** Never put an ID, a user value, or a message ID into a span name, a metric name, or a metric tag.
*   **Enforcement:** `TelemetryResourceTests` checks the resource attributes. No test checks the names, because reflection cannot read a span name or a tag value.
