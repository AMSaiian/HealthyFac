namespace HealthyFac.Insights.IntegrationTests.Database.Data;

public static class DemoIngestSeedTestData
{
    public const int AttemptCount = 3;
    public const int ChunkCount = 101;
    public const int CncChunkCount = 61;
    public const int EmbeddingDimensions = 1536;
    public const double NormTolerance = 1e-5;
    public static readonly Guid CncFileId = Guid.Parse(input: "01a1c5b1-557a-7039-9339-06a308f1f579");
}
