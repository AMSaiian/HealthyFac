using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Runs one Azurite container for the tests of a class. It stores blobs and signs read links with the account key, as
/// the Document service does.
/// </summary>
public sealed class AzuriteBlobFixture : IAsyncLifetime
{
    public const string ContainerName = "files";

    private readonly AzuriteTestStorage _storage = new();

    private BlobContainerClient _container = null!;

    public Uri Address => _storage.Address;

    public async ValueTask InitializeAsync()
    {
        await _storage.StartAsync();

        _container = new BlobServiceClient(_storage.ConnectionString).GetBlobContainerClient(ContainerName);
        await _container.CreateIfNotExistsAsync();
    }

    public ValueTask DisposeAsync() => _storage.DisposeAsync();

    public async Task<Guid> UploadAsync(byte[] content, string contentType)
    {
        var id = Guid.CreateVersion7();

        await _container.GetBlobClient(id.ToString())
            .UploadAsync(
                new BinaryData(content), new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType },
                });

        return id;
    }

    public async Task<byte[]> ReadAsync(Guid id)
    {
        var download = await _container.GetBlobClient(id.ToString()).DownloadContentAsync();

        return download.Value.Content.ToArray();
    }

    public string SignRead(Guid id, DateTimeOffset expiresOn, string? contentType = null, string? disposition = null)
    {
        var builder = new BlobSasBuilder(BlobSasPermissions.Read, expiresOn)
        {
            BlobContainerName = ContainerName,
            BlobName = id.ToString(),
            StartsOn = expiresOn.AddMinutes(minutes: -10),
            ContentType = contentType,
            ContentDisposition = disposition,
        };
        var credential = new StorageSharedKeyCredential(_storage.AccountName, _storage.AccountKey);

        return builder.ToSasQueryParameters(credential).ToString();
    }
}
