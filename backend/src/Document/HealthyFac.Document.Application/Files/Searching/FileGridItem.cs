using System.Linq.Expressions;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Searching;

/// <summary>
/// A row of the file grids.
/// </summary>
public record FileGridItem
{
    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    public string? Description { get; init; }

    public required DateTime UploadedAt { get; init; }

    /// <summary>The row version. A client sends it back with an update.</summary>
    public required uint RowVersion { get; init; }

    /// <summary>
    /// Maps a file ref to its grid row inside a query, so the database reads only these columns.
    /// </summary>
    public static readonly Expression<Func<FileRef, FileGridItem>> Projection =
        file => new FileGridItem
        {
            Id = file.Id,
            FileName = file.FileName,
            Extension = file.Extension,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            Description = file.Description,
            UploadedAt = file.CreatedAt,
            RowVersion = EF.Property<uint>(file, RowVersions.PropertyName),
        };
}
