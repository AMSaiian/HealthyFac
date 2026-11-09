using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files.Storage;

/// <summary>
/// Stores the bytes of a file. The Infrastructure layer implements it with blob storage.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Stores the bytes of a new file under its ID. The write fails when a blob with the ID exists, so a stored file
    /// never changes.
    /// </summary>
    /// <param name="id">The ID of the file ref. It is the name of the blob.</param>
    /// <param name="purpose">What the file is for. The write span carries it.</param>
    /// <param name="content">The bytes to store. The method reads the stream from its current position.</param>
    /// <param name="contentType">The media type that the storage serves with the blob.</param>
    /// <param name="contentDisposition">The <c>Content-Disposition</c> value that the storage serves with the
    /// blob.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="Shared.Application.ErrorHandling.ServiceUnavailableException">
    /// Thrown when the storage is down, rejects the write, or already holds a blob with the ID.
    /// </exception>
    Task WriteAsync(
        Guid id,
        FilePurpose purpose,
        Stream content,
        string contentType,
        string contentDisposition,
        CancellationToken cancellationToken = default(CancellationToken)
    );
}
