---
paths:
  - "backend/src/**"
  - "**/Observability/**"
  - "**/Tracing/**"
  - "**/Metrics/**"
  - "**/Logging/**"
---
# Observability Rules
*   **Resource Attributes:** Set them only in `TelemetryResource` (Shared.Web). Traces, metrics, and logs use it. The deployment or the collector sets platform attributes (`k8s.*`, `host.*`). `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` override the configured values.
*   **Trace Flow:** Every new entry point continues the incoming trace or starts one: an HTTP endpoint (ASP.NET Core instrumentation), a consumed message (a `Consumer` span with the `traceparent` of the message), and a job or a hosted service loop (one span for each run or each unit). Every outgoing call carries the trace: HTTP through the `HttpClient` instrumentation, a message through the `traceparent` header. Add an internal span only for a step with I/O or a measurable cost (rendering, encryption, an SDK without spans). Never add a span for each method.
*   **Own Metrics:** New work that runs outside an HTTP request (a consumer, a job, a processor) records through its own meter, created with `IMeterFactory` (as `MessagingMetrics`): a counter of handled units with a result tag, a duration histogram, and a gauge of the backlog or the work in flight when one exists. An HTTP endpoint needs no own meter, because the ASP.NET Core metrics cover it.
*   **Framework Telemetry:** Register the source and the meter of each referenced library that provides one (ASP.NET Core, `HttpClient`, the runtime, Npgsql). A service registers its own sources and meters with `ConfigureOpenTelemetryTracerProvider` and `ConfigureOpenTelemetryMeterProvider`, or with `AdditionalSources` and `AdditionalMeters`. A new instrumentation package goes to the user (**Library First**).
*   **Tests:** Test each new span with `SpanCapture` (name, kind, parent, tags) and each new instrument with `MetricCollector<T>` (count, value, tags).
*   **Span Names:** Name an internal span `<Area>.<Action>` in PascalCase (`Razor.Render`, `UseCase.Validate`). The use case span keeps the request type name. Library spans keep their names (HTTP, messaging `{operation} {destination}`).
*   **Keys and Names:** Write attribute and tag keys in lower case, with dot namespaces and snake_case words (`healthyfac.use_case`). Name each source and meter after its .NET namespace (`HealthyFac.Messaging`). Define every name in `ObservabilityConstants`.
*   **Metric Names:** Write them in lower case with dots (`healthyfac.messaging.handled`). Put the unit in the instrument (`s`, `{message}`), never in the name.
*   **Cardinality:** Never put an ID, a user value, or a message ID into a span name, a metric name, or a metric tag.
*   **Enforcement:** `TelemetryResourceTests` checks the resource attributes. The tests of each span and instrument (**Tests**) are the check of the names and the tags. The review checks each new entry point for a span. No reflection check can prove that a code path starts an activity.
