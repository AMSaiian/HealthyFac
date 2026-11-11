namespace HealthyFac.Insights.Messaging.Ingest;

/// <summary>
/// The states of an ingest attempt as other services see them. The camelCase values are part of the message and
/// never change in place. The message carries the value as a string, because the messaging JSON options read no
/// enumeration class.
/// </summary>
public static class IngestMessageStates
{
    public const string Queued = "queued";
    public const string InProcess = "inProcess";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Interrupted = "interrupted";

    public static readonly string[] All = [Queued, InProcess, Completed, Failed, Interrupted];
}
