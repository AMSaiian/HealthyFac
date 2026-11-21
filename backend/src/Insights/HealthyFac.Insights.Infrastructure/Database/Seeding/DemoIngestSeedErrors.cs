namespace HealthyFac.Insights.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the failures of the demo ingest seed.
/// </summary>
public static class DemoIngestSeedErrors
{
    public static string AttemptNotCompleted(Guid attemptId) => $"The demo attempt {attemptId} is not completed.";

    public static string RunNotCompleted(Guid runId) => $"The demo run {runId} is not completed.";

    public static string CaseNotOpen(Guid caseId) => $"The demo case {caseId} is not open.";

    public static string SignalNotDrafted(Guid signalId) => $"The demo signal {signalId} is not drafted.";

    public static string SeedFileEmpty(string fileName) => $"The demo seed file {fileName} is empty.";
}
