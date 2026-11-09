namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// Describes one file type that a user may upload.
/// </summary>
public sealed class AllowedFileType
{
    /// <summary>The lower case extension that the service stores, without the dot.</summary>
    public required string Extension { get; init; }

    /// <summary>The media type that the service stores and serves.</summary>
    public required string MediaType { get; init; }

    /// <summary>The other extensions that map to this type.</summary>
    public IReadOnlyList<string> AlternateExtensions { get; init; } = [];

    /// <summary>Whether the type is an image. Only images serve the embedded purpose.</summary>
    public bool IsImage { get; init; }

    /// <summary>
    /// Checks whether a file of this type may have a purpose. Every type serves the attachment purpose. Only images
    /// serve the embedded purpose.
    /// </summary>
    /// <param name="purpose">The purpose of the file.</param>
    /// <returns><see langword="true"/> when the purpose accepts this type.</returns>
    public bool IsAllowedFor(FilePurpose purpose) => purpose == FilePurpose.Attachment || IsImage;
}
