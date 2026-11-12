namespace HealthyFac.Insights.ApiTests.Ingest.Data;

public static class ReadIngestAttemptsTestData
{
    public static string AttemptPath(string encryptedAttemptId) => $"/v1/ingest-attempts/{encryptedAttemptId}";

    public static TheoryData<string, string> RoutesWithoutReadPermission => new()
    {
        { "FileAttempts", StartIngestAttemptTestData.StartPath("{fileId}") },
        { "Attempt", AttemptPath("{attemptId}") },
    };
}
