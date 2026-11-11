namespace HealthyFac.Shared.Infrastructure.Seeding;

/// <summary>
/// Names the demo ingest snapshot in <c>backend/seed/demo-ingest</c>. Every service that seeds a part of it reads the
/// names from here.
/// </summary>
public static class DemoIngestSeedFiles
{
    /// <summary>The folder of the snapshot, relative to the application base, next to the seed data file.</summary>
    public const string RelativeFolder = "seed/demo-ingest";

    public const string DocumentFileName = "document.json";
    public const string InsightsFileName = "insights.json";
    public const string EmbeddingsFileName = "insights-embeddings.json";
    public const string CoreFileName = "core.json";

    /// <summary>The folder of the demo files inside <see cref="RelativeFolder"/>.</summary>
    public const string FilesFolderName = "files";
}
