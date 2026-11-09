namespace HealthyFac.Document.ApiTests.Files.Data;

public static class UploadFileApiTestData
{
    public const string CompanyFilesPath = "v1/files";
    public const string EmbeddedPath = "v1/files/embedded";
    public const string MachineFilesSegment = "files";
    public const string MachinesPath = "v1/machines";
    public const string CompanyWritePermission = "company-files:write";
    public const string FilesWritePermission = "files:write";
    public const string PdfType = "application/pdf";
    public const string PngType = "image/png";
    public const string FileTypeNotAllowed = "fileTypeNotAllowed";
    public const string CrLfDescription = "  first line\r\nsecond line  ";
    public const string NormalizedDescription = "first line\nsecond line";
    public const string DecomposedPdfName = "Cafe\u0301.pdf";
    public const string ComposedBaseName = "Caf\u00e9";
    public const long AttachmentMaxBytes = 104857600;
    public const long EmbeddedMaxBytes = 5242880;
    public const int PermitLimit = 4;
    public const int QueueLimit = 8;

    public static readonly byte[] PdfHeader = [.. "%PDF-1.7\n"u8];

    public static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string MachineFilesPath(string encryptedMachineId) =>
        $"{MachinesPath}/{encryptedMachineId}/{MachineFilesSegment}";
}
