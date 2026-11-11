using System.Linq.Expressions;
using HealthyFac.Document.Application.Files.Ingest;
using HealthyFac.Document.Domain.Files;

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

    /// <summary>The machine of the file, or <see langword="null"/> for a file that no machine owns.</summary>
    public Guid? MachineId { get; init; }

    public string? MachineName { get; init; }

    /// <summary>The ingest state from Insights, or <see langword="null"/> when the file was never ingested.</summary>
    public FileIngestState? IngestState { get; init; }

    /// <summary>
    /// Tells whether the user can start an ingest: an ingestable type, and no attempt that is active or completed. A
    /// failed or interrupted ingest repeats until <see cref="IngestableFileTypes.MaxFailedAttempts"/> attempts failed.
    /// </summary>
    public required bool CanIngest { get; init; }

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
            MachineId = file.MachineId,
            MachineName = file.Machine == null ? null : file.Machine.Name,
            IngestState = file.IngestState,
            CanIngest = IngestableFileTypes.Extensions.Contains(file.Extension)
                && (file.IngestState == null
                    || ((file.IngestState == FileIngestState.Failed || file.IngestState == FileIngestState.Interrupted)
                        && file.IngestFailedCount < IngestableFileTypes.MaxFailedAttempts)),
            RowVersion = file.RowVersion,
        };
}
