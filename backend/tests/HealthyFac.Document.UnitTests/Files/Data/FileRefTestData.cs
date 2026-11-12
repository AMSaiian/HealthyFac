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

    public static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static TheoryData<string, FilePurpose, Guid?, long, byte[]> InvalidConstructorArguments => new()
    {
        { "EmbeddedWithMachine", FilePurpose.Embedded, MachineId, 1, ValidHash },
        { "ZeroSize", FilePurpose.Attachment, null, 0, ValidHash },
        { "ShortHash", FilePurpose.Attachment, null, 1, new byte[FileRef.Sha256Length - 1] },
    };

    public static TheoryData<FileRef, bool> AutomaticTieCases
    {
        get
        {
            var tied = Create(FilePurpose.Attachment);
            tied.TieTo(MachineId, QueuedAt.AddHours(value: -1));

            var untiedAfterQueue = Create(FilePurpose.Attachment);
            untiedAfterQueue.Untie(QueuedAt.AddMinutes(value: 1));

            var untiedBeforeQueue = Create(FilePurpose.Attachment);
            untiedBeforeQueue.Untie(QueuedAt.AddMinutes(value: -1));

            return new TheoryData<FileRef, bool>
            {
                { Create(FilePurpose.Attachment), true },
                { untiedBeforeQueue, true },
                { tied, false },
                { untiedAfterQueue, false },
                { Create(FilePurpose.Embedded), false },
            };
        }
    }
}
