using System.Diagnostics;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Holds the single <see cref="ActivitySource"/> that component rendering traces through. An OpenTelemetry tracer
/// provider picks it up by the source name.
/// </summary>
public static class RenderingActivitySource
{
    /// <summary>The shared activity source for every rendering span.</summary>
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Sources.Rendering);
}
