using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Document.Application.Files.Searching;

/// <summary>
/// Holds the default order of the file grids: the newest upload first.
/// </summary>
public static class FileSearchDefaults
{
    public static readonly GridSort<FileSortColumn> Sort = new()
    {
        Column = FileSortColumn.UploadedAt,
        Direction = SortDirection.Descending,
    };
}
