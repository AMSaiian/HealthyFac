using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files.Uploading;

/// <summary>
/// Stores an uploaded file: it proves the type, writes the blob, and saves the ref. The three upload use cases call it,
/// so the flow has one definition.
/// </summary>
public interface IFileUploader
{
    /// <summary>
    /// Inspects the file, writes the blob, and saves the ref.
    /// </summary>
    /// <param name="companyId">The company that owns the file.</param>
    /// <param name="machineId">The machine of the file, or <see langword="null"/>.</param>
    /// <param name="purpose">What the file is for.</param>
    /// <param name="upload">The uploaded file.</param>
    /// <param name="description">The description, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The ID of the new file ref.</returns>
    Task<Guid> UploadAsync(
        Guid companyId,
        Guid? machineId,
        FilePurpose purpose,
        FileUpload upload,
        string? description,
        CancellationToken cancellationToken
    );
}
