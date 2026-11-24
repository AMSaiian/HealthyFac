namespace HealthyFac.Seeder.IntegrationTests.Ingest.Data;

public static class IngestTestData
{
    public const int FileCount = 6;
    public const int ChunkCount = 206;
    public const int RunCount = 12;
    public const int ToolCallCount = 268;
    public const int MetricDraftCount = 80;
    public const int StatusDraftCount = 168;
    public const int PhaseDraftCount = 14;
    public const int VectorLength = 1536;
    public const string BlobContainer = "files";

    public static readonly IReadOnlyList<string> DocumentPaths =
    [
        "seed/demo/files/halvik-ep-80-200-iom.docx",
        "seed/demo/files/morvane-mtc-420-manual.pdf",
        "seed/demo/files/norrlund-rsv-37-service-data.xlsx",
    ];
}
