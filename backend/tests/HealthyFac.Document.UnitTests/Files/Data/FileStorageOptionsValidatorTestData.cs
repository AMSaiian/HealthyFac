using HealthyFac.Document.Infrastructure.Files.Storage;

namespace HealthyFac.Document.UnitTests.Files.Data;

public static class FileStorageOptionsValidatorTestData
{
    private const string ValidConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=a2V5LXdpdGgtZXF1YWxzPQ==;"
        + "BlobEndpoint=http://localhost:10000/devstoreaccount1;";

    public static TheoryData<FileStorageOptions, string> InvalidOptions => new()
    {
        {
            Create(connectionString: "AccountName=devstoreaccount1;"),
            nameof(FileStorageOptions.ConnectionString)
        },
        {
            Create(connectionString: "AccountKey=a2V5;"),
            nameof(FileStorageOptions.ConnectionString)
        },
        { Create(publicBlobBaseUrl: "/api/document/blobs"), nameof(FileStorageOptions.PublicBlobBaseUrl) },
        { Create(publicBlobBaseUrl: "ftp://api.healthyfac.test/blobs"), nameof(FileStorageOptions.PublicBlobBaseUrl) },
        { Create(linkLifetimeSeconds: 59), nameof(FileStorageOptions.LinkLifetimeSeconds) },
        { Create(linkLifetimeSeconds: 3601), nameof(FileStorageOptions.LinkLifetimeSeconds) },
    };

    public static TheoryData<FileStorageOptions> ValidOptions => new()
    {
        { Create() },
        { Create(linkLifetimeSeconds: 60) },
        { Create(linkLifetimeSeconds: 3600) },
    };

    private static FileStorageOptions Create(
        string connectionString = ValidConnectionString,
        string publicBlobBaseUrl = "https://api.healthyfac.test/api/document/blobs",
        int linkLifetimeSeconds = 300
    ) =>
        new()
        {
            ConnectionString = connectionString,
            PublicBlobBaseUrl = publicBlobBaseUrl,
            LinkLifetimeSeconds = linkLifetimeSeconds,
        };
}
