namespace HealthyFac.Seeder.Ingest;

/// <summary>
/// The content of an ingest set file (<c>ingest/set-1.json</c>): one entry for each ingested document.
/// </summary>
public sealed class IngestSetFile
{
    public required IReadOnlyList<IngestEntry> Entries { get; init; }
}
