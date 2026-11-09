using FluentValidation.Results;
using HealthyFac.Document.Application.Files.Inspection;
using HealthyFac.Document.Application.Files.Storage;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.Application.Files.Uploading;

/// <summary>
/// Stores an uploaded file in this order: inspect, write the blob, save the ref.
/// </summary>
/// <remarks>
/// The blob write is a durable step that a rollback cannot undo. The save after it uses the form <b>Finish</b> (rule
/// <b>Cancellation Tokens</b>): it passes <see cref="CancellationToken.None"/>, so a stored blob always gets its ref.
/// A failed save leaves an orphan blob, which the service accepts until the blob cleanup exists. The write itself
/// observes the token, and so does the inspection, so a cancel before the blob write stores nothing.
/// </remarks>
/// <param name="inspector">Proves the type of the file.</param>
/// <param name="storage">Writes the blob.</param>
/// <param name="dbContext">Saves the ref.</param>
public sealed class FileUploader(IFileInspector inspector, IFileStorage storage, IDocumentDbContext dbContext)
    : IFileUploader
{
    private const string FileKey = "file";

    private readonly IFileInspector _inspector = inspector;
    private readonly IFileStorage _storage = storage;
    private readonly IDocumentDbContext _dbContext = dbContext;

    /// <summary>
    /// Inspects the file, writes the blob, and saves the ref.
    /// </summary>
    /// <param name="companyId">The company that owns the file.</param>
    /// <param name="machineId">The machine of the file, or <see langword="null"/>.</param>
    /// <param name="purpose">What the file is for.</param>
    /// <param name="upload">The uploaded file. The validator has checked the name and the size.</param>
    /// <param name="description">The description, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The ID of the new file ref.</returns>
    /// <exception cref="AppValidationException">
    /// Thrown when the content, the extension, the declared type, and the purpose do not agree on an accepted type.
    /// </exception>
    public async Task<Guid> UploadAsync(
        Guid companyId,
        Guid? machineId,
        FilePurpose purpose,
        FileUpload upload,
        string? description,
        CancellationToken cancellationToken
    )
    {
        var inspection = await _inspector.InspectAsync(
            upload.Content,
            FileNames.ToExtension(upload.ClientFileName),
            upload.DeclaredContentType,
            purpose,
            cancellationToken);

        if (inspection.Type is not { } type)
        {
            throw new AppValidationException([new ValidationFailure(FileKey, FileErrors.TypeNotAllowed)]);
        }

        var baseName = FileNames.ToBaseName(upload.ClientFileName)!;
        var fileRef = new FileRef(
            companyId,
            machineId,
            purpose,
            baseName,
            type.Extension,
            type.MediaType,
            inspection.SizeBytes,
            inspection.Sha256,
            description);

        await _storage.WriteAsync(
            fileRef.Id,
            purpose,
            upload.Content,
            type.MediaType,
            FileContentDisposition.Build(type, baseName),
            cancellationToken);

        _dbContext.FileRefs.Add(fileRef);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        return fileRef.Id;
    }
}
