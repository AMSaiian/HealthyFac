namespace HealthyFac.Seeder.Ingest;

public sealed class IngestChunkSeed
{
    public required Guid Id { get; init; }

    public required int Ordinal { get; init; }

    public string? Location { get; init; }

    public required string Text { get; init; }
}
