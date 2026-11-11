namespace HealthyFac.Insights.Application.Files;

/// <summary>
/// The data of a file that Document owns, as Insights needs it to start and run an ingest.
/// </summary>
public record IngestFile
{
    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    /// <summary>The machine that the file is tied to, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }

    /// <summary>The time of the last tie change, or the file creation time when never tied (UTC).</summary>
    public required DateTime MachineChangedAt { get; init; }

    public required string Purpose { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required long SizeBytes { get; init; }
}
