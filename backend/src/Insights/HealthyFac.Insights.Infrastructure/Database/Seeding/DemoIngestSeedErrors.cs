namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the failures of the demo ingest seed.
/// </summary>
public static class DemoIngestSeedErrors
{
    public static string AttemptNotCompleted(Guid attemptId) => $"The demo attempt {attemptId} is not completed.";

    public static string SeedFileEmpty(string fileName) => $"The demo seed file {fileName} is empty.";
}
