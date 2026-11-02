using System.Diagnostics;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Holds the single <see cref="ActivitySource"/> that email sending traces through. An OpenTelemetry tracer
/// provider picks it up by the source name.
/// </summary>
public static class EmailActivitySource
{
    /// <summary>The shared activity source for every email span.</summary>
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Sources.Email);
}
