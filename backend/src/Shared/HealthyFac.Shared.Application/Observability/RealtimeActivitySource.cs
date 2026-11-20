using System.Diagnostics;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Holds the single <see cref="ActivitySource"/> of the real-time push. An OpenTelemetry tracer provider picks it up
/// by the source name.
/// </summary>
public static class RealtimeActivitySource
{
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Sources.Realtime);
}
