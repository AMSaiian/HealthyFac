using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files.Inspection;

/// <summary>
/// Proves the type of a file from its bytes. The client name and the declared type only have to agree with the proof.
/// </summary>
public interface IFileInspector
{
    /// <summary>
    /// Reads the content once for the hash and the size, detects the type from the bytes, and checks that the client
    /// extension, the declared type, and the purpose agree with it.
    /// </summary>
    /// <param name="seekableContent">The content. The stream position is 0 after the call.</param>
    /// <param name="clientExtension">The extension of the client file name without the dot, or
    /// <see langword="null"/> when the name has none.</param>
    /// <param name="declaredContentType">The media type that the client declared.</param>
    /// <param name="purpose">What the file is for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The inspection. <see cref="FileInspection.Type"/> is <see langword="null"/> for a refused
    /// file.</returns>
    Task<FileInspection> InspectAsync(
        Stream seekableContent,
        string? clientExtension,
        string declaredContentType,
        FilePurpose purpose,
        CancellationToken cancellationToken = default(CancellationToken)
    );
}
