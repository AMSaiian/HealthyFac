namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class BlobFileStorageTestData
{
    public const string ContentType = "application/pdf";
    public const string ContentDisposition = "attachment; filename=\"report.pdf\"";

    public static readonly byte[] Content = [1, 2, 3, 4, 5];
}
