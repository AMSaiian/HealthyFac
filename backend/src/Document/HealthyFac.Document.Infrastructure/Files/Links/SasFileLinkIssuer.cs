using Azure.Storage;
using Azure.Storage.Sas;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Files.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.Infrastructure.Files.Links;

/// <summary>
/// Signs a service SAS for one blob with the storage account key. The link reads the blob and sets the type and the
/// disposition of the response. The call makes no storage request.
/// </summary>
/// <remarks>
/// The protocol is HTTPS only, except in Development: there the gateway reaches Azurite over HTTP, and Azurite checks
/// the protocol of the request that reaches it. The SAS is a bearer secret, so no code logs the URL.
/// </remarks>
/// <param name="options">The file storage settings.</param>
/// <param name="timeProvider">Provides the start and the expiry of the link.</param>
/// <param name="environment">Decides the allowed protocol.</param>
public sealed class SasFileLinkIssuer(
    IOptions<FileStorageOptions> options,
    TimeProvider timeProvider,
    IHostEnvironment environment
) : IFileLinkIssuer
{
    private const string BlobSasResource = "b";
    private const char UrlSeparator = '/';

    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(minutes: 1);

    private readonly FileStorageOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    private readonly SasProtocol _protocol = environment.IsDevelopment()
        ? SasProtocol.HttpsAndHttp
        : SasProtocol.Https;

    private readonly StorageSharedKeyCredential _credential = CreateCredential(options.Value.ConnectionString);

    public FileLink Issue(FileRef file)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresOn = now.AddSeconds(_options.LinkLifetimeSeconds);
        var type = AllowedFileTypes.FindByExtension(file.Extension)!;
        var builder = new BlobSasBuilder(BlobSasPermissions.Read, expiresOn)
        {
            BlobContainerName = _options.ContainerName,
            BlobName = file.Id.ToString(),
            Resource = BlobSasResource,
            StartsOn = now - ClockSkew,
            Protocol = _protocol,
            ContentType = file.ContentType,
            ContentDisposition = FileContentDisposition.Build(type, file.FileName),
        };
        var signature = builder.ToSasQueryParameters(_credential);
        var baseUrl = _options.PublicBlobBaseUrl.TrimEnd(UrlSeparator);

        return new FileLink
        {
            Id = file.Id,
            Url = $"{baseUrl}/{_options.ContainerName}/{file.Id}?{signature}",
            ExpiresAt = expiresOn.UtcDateTime,
        };
    }

    private static StorageSharedKeyCredential CreateCredential(string connectionString)
    {
        var settings = FileStorageConnectionString.Parse(connectionString);

        return new StorageSharedKeyCredential(
            settings[FileStorageConstants.ConnectionStringAccountName],
            settings[FileStorageConstants.ConnectionStringAccountKey]);
    }
}
