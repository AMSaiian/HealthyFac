using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Defines the service actors of the ingest worker. The sweep and the claim run as a service flow. The run of an
/// attempt restores the actor of the user who queued it.
/// </summary>
public static class IngestServiceActors
{
    public const string WorkerFlow = "ingestWorker";

    /// <summary>The actor of the sweep and the claim: the flow <c>insights:ingestWorker</c>.</summary>
    public static ActorContext Worker { get; } = KnownActors.ForServiceFlow(ServiceName.Insights, WorkerFlow);
}
