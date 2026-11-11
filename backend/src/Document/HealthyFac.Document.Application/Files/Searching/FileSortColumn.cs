using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Document.Application.Files.Searching;

/// <summary>
/// The sortable columns of the file grids.
/// </summary>
public sealed class FileSortColumn : Enumeration<FileSortColumn>
{
    public static readonly FileSortColumn FileName = new(value: "fileName");
    public static readonly FileSortColumn SizeBytes = new(value: "sizeBytes");
    public static readonly FileSortColumn UploadedAt = new(value: "uploadedAt");
    public static readonly FileSortColumn MachineName = new(value: "machineName");

    private FileSortColumn(string value)
        : base(value)
    {
    }
}
