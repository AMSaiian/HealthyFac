using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files.Inspection;

/// <summary>
/// The result of the inspection of a file.
/// </summary>
public sealed class FileInspection
{
    /// <summary>
    /// The type that the content proved, or <see langword="null"/> when the service does not accept the file for the
    /// purpose.
    /// </summary>
    public AllowedFileType? Type { get; init; }

    /// <summary>The size of the content in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>The SHA-256 hash of the content.</summary>
    public required byte[] Sha256 { get; init; }
}
