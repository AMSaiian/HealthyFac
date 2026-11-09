using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// The record of a stored file: the owner, the name, the type, the size, and the hash. The bytes live in blob
/// storage under the ID of the ref. A removal keeps the row (soft delete).
/// </summary>
public class FileRef : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>The length of a SHA-256 hash in bytes.</summary>
    public const int Sha256Length = 32;

    /// <summary>
    /// Creates the ref of a stored file.
    /// </summary>
    /// <param name="companyId">The company that owns the file.</param>
    /// <param name="machineId">The machine of the file, or <see langword="null"/> for a company file.</param>
    /// <param name="purpose">What the file is for.</param>
    /// <param name="fileName">The name without the extension.</param>
    /// <param name="extension">The stored lower case extension without the dot.</param>
    /// <param name="contentType">The stored media type.</param>
    /// <param name="sizeBytes">The size of the file in bytes.</param>
    /// <param name="sha256">The SHA-256 hash of the bytes.</param>
    /// <param name="description">The description, or <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="FileInvariantErrors"/> when an embedded image has a machine, the size is not above 0, or
    /// the hash does not have <see cref="Sha256Length"/> bytes.
    /// </exception>
    public FileRef(
        Guid companyId,
        Guid? machineId,
        FilePurpose purpose,
        string fileName,
        string extension,
        string contentType,
        long sizeBytes,
        byte[] sha256,
        string? description
    )
    {
        if (purpose == FilePurpose.Embedded && machineId is not null)
        {
            throw FileInvariantErrors.EmbeddedFileWithMachine();
        }

        if (sizeBytes <= 0)
        {
            throw FileInvariantErrors.EmptyFile();
        }

        if (sha256.Length != Sha256Length)
        {
            throw FileInvariantErrors.InvalidHash(Sha256Length);
        }

        CompanyId = companyId;
        MachineId = machineId;
        Purpose = purpose;
        FileName = fileName;
        Extension = extension;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        Description = description;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid CompanyId { get; private set; }
    public Guid? MachineId { get; private set; }
    public FilePurpose Purpose { get; private set; }
    public string FileName { get; private set; }
    public string Extension { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public byte[] Sha256 { get; private set; }
    public string? Description { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the data that a user edits. The type, the size, and the hash never change.
    /// </summary>
    /// <param name="fileName">The new name without the extension.</param>
    /// <param name="description">The new description, or <see langword="null"/>.</param>
    public void Rename(string fileName, string? description)
    {
        FileName = fileName;
        Description = description;
    }
}
