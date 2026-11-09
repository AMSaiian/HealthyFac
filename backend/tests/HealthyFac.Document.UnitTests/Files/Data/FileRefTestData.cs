using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.UnitTests.Files.Data;

public static class FileRefTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000002");

    public static byte[] ValidHash => new byte[FileRef.Sha256Length];

    public static FileRef Create(
        FilePurpose purpose,
        Guid? machineId = null,
        long sizeBytes = 1,
        byte[]? sha256 = null
    ) =>
        new(
            CompanyId,
            machineId,
            purpose,
            fileName: "report",
            extension: "png",
            contentType: "image/png",
            sizeBytes,
            sha256 ?? ValidHash,
            description: null);
}
