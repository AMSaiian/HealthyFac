using Google.Protobuf.WellKnownTypes;
using HealthyFac.Document.Contracts.Files;
using ContractFileInfo = HealthyFac.Document.Contracts.Files.FileInfo;

namespace HealthyFac.Insights.UnitTests.Files.Data;

public static class DocumentCatalogTestData
{
    public const string LinkUrl = "https://files.test/signed";

    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    public static readonly DateTime ChangedAt = new(
        year: 2026, month: 11, day: 9, hour: 8, minute: 30, second: 0, DateTimeKind.Utc);

    public static readonly DateTime ExpiresAt = ChangedAt.AddMinutes(value: 15);

    public static ContractFileInfo CreateInfo(Guid? machineId)
    {
        var info = new ContractFileInfo
        {
            FileId = FileId.ToString(),
            CompanyId = CompanyId.ToString(),
            MachineChangedAt = Timestamp.FromDateTime(ChangedAt),
            Purpose = "attachment",
            FileName = "manual",
            Extension = "pdf",
            SizeBytes = 2048,
        };

        if (machineId is not null)
        {
            info.MachineId = machineId.ToString();
        }

        return info;
    }

    public static FileLinkReply CreateLink(Guid? machineId) =>
        new()
        {
            File = CreateInfo(machineId),
            Url = LinkUrl,
            ExpiresAt = Timestamp.FromDateTime(ExpiresAt),
        };
}
