using HealthyFac.Document.Application.Files.Uploading;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class UploadFileTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a11");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a12");

    public const string PdfName = "Pump manual.v2.pdf";
    public const string PdfType = "application/pdf";
    public const string PngName = "scheme.png";
    public const string PngType = "image/png";
    public const string ExeNameAsPdf = "virus.pdf";
    public const string NameOnlyExtension = ".pdf";
    public const string Description = "Maintenance manual";
    public const string StoredPdfName = "Pump manual.v2";

    private static readonly byte[] Padding = new byte[64];

    public static byte[] Pdf => [.. "%PDF-1.7\n"u8, .. Padding];

    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Padding];

    public static byte[] Exe => [0x4D, 0x5A, 0x90, 0x00, .. Padding];

    public static TheoryData<string> OutOfScopeMachines => new()
    {
        { OutOfScopeCases.OtherCompany },
        { OutOfScopeCases.DeletedRegistration },
        { OutOfScopeCases.UnknownMachine },
    };

    public static FileUpload CreateUpload(string name, byte[] content, string declaredType) =>
        new()
        {
            Content = new MemoryStream(content),
            ClientFileName = name,
            DeclaredContentType = declaredType,
            Length = content.Length,
        };

    public static class OutOfScopeCases
    {
        public const string OtherCompany = "otherCompany";
        public const string DeletedRegistration = "deletedRegistration";
        public const string UnknownMachine = "unknownMachine";
    }
}
