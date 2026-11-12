using HealthyFac.Document.Application.Files;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class UpdateFileTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e21");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e22");
    public const string Description = "New description";
    public const string FormattedDescription = "<p><strong>Bold</strong> text</p>";
    public const string ScriptDescription = "<p>a</p><script>alert(1)</script>";
    public const int RaceParticipantCount = 2;

    public static readonly DateTime UploadedAt = new(
        year: 2026, month: 4, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static FileSeed CompanyFile(string extension = "pdf") =>
        new(FileName: "old", extension, SizeBytes: 10, UploadedAt);

    public static UpdateFileRequest CompanyUpdate(
        Guid id,
        uint rowVersion,
        string fileName = "new",
        string? description = null,
        Guid? companyId = null
    ) =>
        new()
        {
            CompanyId = companyId ?? CompanyId,
            Id = id,
            FileName = fileName,
            Description = description,
            RowVersion = rowVersion,
        };

    public static UpdateFileRequest MachineUpdate(
        Guid machineId,
        Guid id,
        uint rowVersion,
        string fileName = "new"
    ) =>
        new()
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            Id = id,
            FileName = fileName,
            RowVersion = rowVersion,
        };
}
