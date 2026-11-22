using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Ingest;

/// <summary>One uploaded document with the result of its ingest.</summary>
public sealed class IngestEntry
{
    public required IngestFileSeed File { get; init; }

    /// <summary>The time when the user queued the ingest. The times inside the entry are offsets after it.</summary>
    public required RelativeMoment QueuedAt { get; init; }

    public required IngestAttemptSeed Attempt { get; init; }

    public required IReadOnlyList<IngestChunkSeed> Chunks { get; init; }

    public required IReadOnlyList<IngestRunSeed> Runs { get; init; }

    public required IngestDraftsSeed DraftsProposed { get; init; }
}
