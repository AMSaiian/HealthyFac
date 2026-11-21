namespace HealthyFac.Insights.IntegrationTests.Database.Data;

public static class DemoIngestSeedTestData
{
    public const int AttemptCount = 3;
    public const int ChunkCount = 103;
    public const int RunCount = 6;
    public const int ToolCallCount = 134;
    public const int CncChunkCount = 62;
    public const int EmbeddingDimensions = 1536;
    public const double NormTolerance = 1e-5;
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    public static readonly Guid CaseId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c41");
    public static readonly Guid SignalId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c42");
    public static readonly Guid CncFileId = Guid.Parse(input: "01a1efde-2242-73fb-8578-c4961231a06f");
}
