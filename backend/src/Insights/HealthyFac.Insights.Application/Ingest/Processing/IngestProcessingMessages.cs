namespace HealthyFac.Insights.Application.Ingest.Processing;

public static class IngestProcessingMessages
{
    public const string TimeoutInvalid = "Ingest:TimeoutSeconds must be above 0.";
    public const string LeaseMarginInvalid = "Ingest:LeaseMarginSeconds must be above 0.";
    public const string PollInvalid = "Ingest:PollSeconds must be above 0.";
    public const string WorkerCountInvalid = "Ingest:WorkerCount must be 1.";
    public const string MaxPagesInvalid = "Ingest:MaxPages must be above 0.";
    public const string RetryDelayInvalid = "Ingest:RetryDelayMilliseconds must be above 0.";
}
