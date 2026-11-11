using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// The ingest state of a file, copied from Insights. The camelCase values reach the database and equal the values of
/// the ingest state in the Insights messages, so never rename one in place.
/// </summary>
public sealed class FileIngestState : Enumeration<FileIngestState>
{
    public static readonly FileIngestState Queued = new(value: "queued");
    public static readonly FileIngestState InProcess = new(value: "inProcess");
    public static readonly FileIngestState Completed = new(value: "completed");
    public static readonly FileIngestState Failed = new(value: "failed");
    public static readonly FileIngestState Interrupted = new(value: "interrupted");

    private FileIngestState(string value)
        : base(value)
    {
    }
}
