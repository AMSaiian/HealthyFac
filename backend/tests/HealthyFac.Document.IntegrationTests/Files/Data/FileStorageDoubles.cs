using HealthyFac.Document.Application.Files.Storage;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

/// <summary>
/// A storage that always fails, like a storage that is down.
/// </summary>
public sealed class FailingFileStorage : IFileStorage
{
    public Task WriteAsync(
        Guid id,
        FilePurpose purpose,
        Stream content,
        string contentType,
        string contentDisposition,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => throw new ServiceUnavailableException(message: "The test storage is down.");
}

/// <summary>
/// A storage that stores the write in the inner storage and then cancels the request, like a client that disconnects
/// right after the blob write.
/// </summary>
/// <param name="inner">The storage that stores the bytes.</param>
/// <param name="cancellation">The source to cancel after the write.</param>
public sealed class CancellingFileStorage(IFileStorage inner, CancellationTokenSource cancellation) : IFileStorage
{
    private readonly IFileStorage _inner = inner;
    private readonly CancellationTokenSource _cancellation = cancellation;

    public async Task WriteAsync(
        Guid id,
        FilePurpose purpose,
        Stream content,
        string contentType,
        string contentDisposition,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        await _inner.WriteAsync(id, purpose, content, contentType, contentDisposition, cancellationToken);
        await _cancellation.CancelAsync();
    }
}
