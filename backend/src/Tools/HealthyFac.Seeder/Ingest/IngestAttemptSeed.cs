namespace HealthyFac.Seeder.Ingest;

public sealed class IngestAttemptSeed
{
    public required Guid Id { get; init; }

    public required TimeSpan StartedAfter { get; init; }

    public required TimeSpan FinishedAfter { get; init; }

    public required int PageCount { get; init; }

    public required int ProposedDraftCount { get; init; }
}
