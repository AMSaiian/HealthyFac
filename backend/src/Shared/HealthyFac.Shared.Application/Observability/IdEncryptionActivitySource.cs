using System.Diagnostics;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Holds the <see cref="ActivitySource"/> that identifier encryption traces through. A separate source
/// lets a tracer provider keep or drop these frequent, short spans without touching the use case spans.
/// </summary>
public static class IdEncryptionActivitySource
{
    /// <summary>The shared activity source for identifier encryption and decryption.</summary>
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Sources.IdEncryption);
}
