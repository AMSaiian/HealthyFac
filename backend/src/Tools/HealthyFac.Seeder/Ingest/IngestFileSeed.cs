using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Ingest;

public sealed class IngestFileSeed
{
    /// <summary>The key of the file ID in the seed context.</summary>
    public required string Key { get; init; }

    /// <summary>The path of the document, relative to the seed folder.</summary>
    public required string Path { get; init; }

    public required RelativeMoment UploadedAt { get; init; }

    /// <summary>The key of the person that uploads the file and queues its ingest.</summary>
    public required string UploadedBy { get; init; }
}
