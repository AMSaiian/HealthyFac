using System.Diagnostics;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Holds the single <see cref="ActivitySource"/> that use case handlers trace through.
/// An OpenTelemetry tracer provider picks it up by the source name, so no exporter is required for the
/// instrumentation to be correct.
/// </summary>
public static class UseCaseActivitySource
{
    /// <summary>The shared activity source for every use case handler.</summary>
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Sources.UseCases);
}
