using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;

namespace HealthyFac.Document.Infrastructure.Observability;

/// <summary>
/// Holds the activity source of the Document file handling. The service registers it in the tracing setup through
/// <c>Tracing:AdditionalSources</c>.
/// </summary>
public static class DocumentActivitySource
{
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Files.Source);
}
