using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files.Searching;

/// <summary>
/// Maps each sort column of the file grids to its key expression.
/// </summary>
public static class FileSortMap
{
    public static readonly FrozenDictionary<FileSortColumn, Expression<Func<FileRef, dynamic>>> Instance =
        new Dictionary<FileSortColumn, Expression<Func<FileRef, dynamic>>>
        {
            [FileSortColumn.FileName] = file => file.FileName,
            [FileSortColumn.SizeBytes] = file => file.SizeBytes,
            [FileSortColumn.UploadedAt] = file => file.CreatedAt,
            [FileSortColumn.MachineName] = file => file.Machine!.Name,
        }.ToFrozenDictionary();
}
