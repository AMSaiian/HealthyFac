using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class CompanyFilesGridTestData
{
    public const string PumpName = "Pump";
    public const string LatheName = "Lathe";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000d11");

    public static readonly DateTime UploadedAt =
        new(year: 2026, month: 3, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static IReadOnlyList<FileSeed> CreateSeeds(Guid? tiedMachineId) =>
    [
        new(FileName: "first", Extension: "pdf", SizeBytes: 10, UploadedAt, tiedMachineId),
        new(FileName: "second", Extension: "pdf", SizeBytes: 10, UploadedAt.AddDays(value: 1)),
        new(FileName: "third", Extension: "docx", SizeBytes: 10, UploadedAt.AddDays(value: 2)),
    ];

    public static TheoryData<string, string?, int, bool> CanIngestCases => new()
    {
        { "pdf", null, 0, true },
        { "png", null, 0, false },
        { "pdf", IngestMessageStates.Queued, 0, false },
        { "pdf", IngestMessageStates.InProcess, 0, false },
        { "pdf", IngestMessageStates.Completed, 0, false },
        { "pdf", IngestMessageStates.Failed, 2, true },
        { "pdf", IngestMessageStates.Failed, 3, false },
        { "pdf", IngestMessageStates.Interrupted, 2, true },
        { "pdf", IngestMessageStates.Interrupted, 3, false },
    };
}
