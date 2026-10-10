namespace HealthyFac.Shared.Web.Logging;

/// <summary>
/// Configures the OpenTelemetry log exporter that Serilog writes to.
/// Leave <see cref="Endpoint"/> empty to keep the exporter off.
/// </summary>
public sealed class OpenTelemetryLoggingOptions
{
    public const string SectionName = "OpenTelemetryLogging";

    /// <summary>The OTLP collector endpoint. An empty value disables the exporter.</summary>
    public string? Endpoint { get; init; }

    /// <summary>The logical service name reported as the OpenTelemetry resource.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Set to false to keep the exporter off even when an endpoint is configured.</summary>
    public bool Enabled { get; init; } = true;
}
