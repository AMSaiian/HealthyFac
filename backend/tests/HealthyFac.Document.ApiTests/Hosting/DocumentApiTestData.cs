using HealthyFac.Testing.Authentication;

namespace HealthyFac.Document.ApiTests.Hosting;

public static class DocumentApiTestData
{
    public const string QueueNamePrefix = "document-api-tests-";
    public const string PublicBlobBaseUrl = "https://api.healthyfac.test/api/document/blobs";

    public const string UnreachableStorageConnection =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=AAAA;"
        + "BlobEndpoint=http://127.0.0.1:1/devstoreaccount1;";

    /// <summary>
    /// A storage endpoint on a non-routable address: the connection hangs instead of failing at once.
    /// </summary>
    public const string HangingStorageConnection =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=AAAA;"
        + "BlobEndpoint=http://10.255.255.1:81/devstoreaccount1;";

    /// <summary>
    /// The timeout of the container readiness probe. It mirrors <c>ProbeConstants.TimeoutSeconds</c> of
    /// <c>HealthyFac.HealthProbe</c>, which the <c>HEALTHCHECK</c> of each app image runs.
    /// </summary>
    public static readonly TimeSpan ReadinessProbeTimeout = TimeSpan.FromSeconds(seconds: 3);

    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";

    public static IReadOnlyDictionary<string, string?> CreateSettings(
        string databaseConnection,
        string brokerConnection,
        string storageConnection
    )
        => new Dictionary<string, string?>(TestTokenFactory.JwtSettings)
        {
            [key: "ConnectionStrings:Database"] = databaseConnection,
            [key: "ConnectionStrings:RabbitMq"] = brokerConnection,
            [key: "FileStorage:ConnectionString"] = storageConnection,
            [key: "FileStorage:PublicBlobBaseUrl"] = PublicBlobBaseUrl,
            [key: "Messaging:QueueName"] = $"{QueueNamePrefix}{Guid.NewGuid():N}",
            [key: "IdEncryption:Keys:0:Prefix"] = "v1.",
            [key: "IdEncryption:Keys:0:Key"] = IdEncryptionKey,
            [key: "IdEncryption:Keys:0:IsDefault"] = bool.TrueString,
            [key: "Seeding:IsOptionalDataSeeded"] = bool.FalseString,
        };
}
