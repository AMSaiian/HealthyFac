using System.Diagnostics;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HealthyFac.Document.Application.Files.Storage;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Observability;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Stores the bytes of the files as blobs in one container. The blob name is the ID of the file ref.
/// </summary>
/// <param name="serviceClient">The shared blob service client.</param>
/// <param name="options">The file storage settings.</param>
/// <param name="logger">Logs a storage failure without the file name.</param>
public sealed class BlobFileStorage(
    BlobServiceClient serviceClient,
    IOptions<FileStorageOptions> options,
    ILogger<BlobFileStorage> logger
) : IFileStorage
{
    private readonly BlobContainerClient _container =
        serviceClient.GetBlobContainerClient(options.Value.ContainerName);

    private readonly ILogger<BlobFileStorage> _logger = logger;

    /// <summary>
    /// Stores the bytes with a condition that no blob with the ID exists. A storage failure and a transport failure
    /// both give <see cref="ServiceUnavailableException"/> and keep the original exception inside. A cancellation
    /// passes through.
    /// </summary>
    /// <param name="id">The ID of the file ref.</param>
    /// <param name="purpose">What the file is for. The span carries it.</param>
    /// <param name="content">The bytes to store.</param>
    /// <param name="contentType">The media type that the storage serves.</param>
    /// <param name="contentDisposition">The <c>Content-Disposition</c> that the storage serves.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when the storage call fails.</exception>
    public async Task WriteAsync(
        Guid id,
        FilePurpose purpose,
        Stream content,
        string contentType,
        string contentDisposition,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        using var activity = DocumentActivitySource.Instance.StartActivity(
            ObservabilityConstants.Files.Spans.BlobUpload, ActivityKind.Internal);
        activity?.SetTag(ObservabilityConstants.Files.Tags.Purpose, purpose.Value);

        var uploadOptions = new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType, ContentDisposition = contentDisposition },
        };

        try
        {
            await _container.GetBlobClient(id.ToString()).UploadAsync(content, uploadOptions, cancellationToken);
        }
        catch (Exception exception)
            when (exception is RequestFailedException or HttpRequestException or IOException or AggregateException
                && !cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            _logger.LogWarning(
                message: "The file storage rejected a write: {ExceptionType} {Status}",
                exception.GetType().Name,
                (exception as RequestFailedException)?.Status);

            throw new ServiceUnavailableException(FileStorageErrors.Unavailable, exception);
        }
    }

    /// <summary>
    /// Tells whether a blob with the ID exists.
    /// </summary>
    /// <param name="id">The ID of the file ref.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="true"/> when the blob exists.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var response = await _container.GetBlobClient(id.ToString()).ExistsAsync(cancellationToken);

        return response.Value;
    }

    /// <summary>
    /// Creates the container when it is missing, with no public access. It is safe to run again and in parallel.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureContainerAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reads the properties of the container, which proves that the storage answers and the container exists.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task CheckContainerAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        await _container.GetPropertiesAsync(cancellationToken: cancellationToken);
    }
}
