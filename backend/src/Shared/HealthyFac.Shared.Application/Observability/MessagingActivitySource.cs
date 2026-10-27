using System.Diagnostics;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Holds the single <see cref="ActivitySource"/> that the outbox publisher, the transport, and the inbox runner
/// trace through. An OpenTelemetry tracer provider picks it up by the source name.
/// </summary>
public static class MessagingActivitySource
{
    /// <summary>The shared activity source for every messaging span.</summary>
    public static readonly ActivitySource Instance = new(ObservabilityConstants.Sources.Messaging);
}
