using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;

namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Holds the activity source of the ingest. The service registers it in the tracing setup through
/// <c>Tracing:AdditionalSources</c>.
/// </summary>
public static class IngestActivitySource
{
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Ingest.Source);
}
