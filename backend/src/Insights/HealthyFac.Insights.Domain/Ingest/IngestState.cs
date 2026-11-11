using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Ingest;

/// <summary>
/// Defines the states of an ingest attempt. The camelCase values reach the API, the messages, and the database, so
/// never rename one in place.
/// </summary>
public sealed class IngestState : Enumeration<IngestState>
{
    public static readonly IngestState Queued = new(value: "queued");
    public static readonly IngestState InProcess = new(value: "inProcess");
    public static readonly IngestState Completed = new(value: "completed");
    public static readonly IngestState Failed = new(value: "failed");
    public static readonly IngestState Interrupted = new(value: "interrupted");

    private IngestState(string value)
        : base(value)
    {
    }
}
