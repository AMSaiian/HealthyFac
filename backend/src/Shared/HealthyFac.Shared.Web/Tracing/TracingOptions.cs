namespace HealthyFac.Shared.Web.Tracing;

/// <summary>
/// Configures the OpenTelemetry tracer and its OTLP exporter.
/// Keep <see cref="Enabled"/> true in every environment. Spans stamp each log entry with a trace
/// identifier even when no collector receives them. Set <see cref="Endpoint"/> to export the spans.
/// </summary>
public sealed class TracingOptions
{
    public const string SectionName = "Tracing";

    /// <summary>Set to false only to switch tracing off entirely, for example during a load test.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>The OTLP collector endpoint. An empty value keeps the spans in process.</summary>
    public string? Endpoint { get; init; }

    /// <summary>The logical service name reported as the OpenTelemetry resource.</summary>
    public string? ServiceName { get; init; }

    /// <summary>
    /// The fraction of traces to record, from 0.0 to 1.0. Lower this when span volume costs too much.
    /// A sampled parent always wins, so a trace never breaks apart across services.
    /// </summary>
    public double SamplingRatio { get; init; } = 1.0;

    /// <summary>Extra activity source names to listen to, beyond the shared use case source.</summary>
    public string[] AdditionalSources { get; init; } = [];
}
