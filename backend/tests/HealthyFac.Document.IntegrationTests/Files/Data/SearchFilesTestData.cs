using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class SearchFilesTestData
{
    public const string LowerAlphaPrefix = "alpha";
    public const int PageSize = 2;
    public const int SecondPageOffset = 2;

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c11");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c12");

    public static readonly DateTime FirstUploadedAt = new(
        year: 2026, month: 3, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static IReadOnlyList<FileSeed> CreateSeeds(Guid? machineId = null) =>
    [
        new(FileName: "Alpha manual", Extension: "pdf", SizeBytes: 300, FirstUploadedAt, machineId),
        new(FileName: "alpha scheme", Extension: "png", SizeBytes: 100, FirstUploadedAt.AddDays(value: 1), machineId),
        new(FileName: "Beta", Extension: "pdf", SizeBytes: 200, FirstUploadedAt.AddDays(value: 2), machineId),
        new(FileName: "Gamma", Extension: "docx", SizeBytes: 400, FirstUploadedAt.AddDays(value: 3), machineId),
    ];

    public static SearchFilesRequest CreateCompanySearch(
        Guid companyId,
        FileGridFilter? filter = null,
        GridSort<FileSortColumn>? sort = null,
        int offset = 0,
        int limit = 25
    ) =>
        new()
        {
            CompanyId = companyId,
            Offset = offset,
            Limit = limit,
            Sort = sort,
            Filter = filter ?? new FileGridFilter(),
        };

    public static SearchFilesRequest CreateMachineSearch(
        Guid companyId,
        Guid machineId,
        FileGridFilter? filter = null
    ) =>
        new()
        {
            CompanyId = companyId,
            MachineId = machineId,
            Offset = 0,
            Limit = 25,
            Filter = filter ?? new FileGridFilter(),
        };

    public static TheoryData<FileSortColumn, SortDirection, int[]> SortCases => new()
    {
        { FileSortColumn.FileName, SortDirection.Ascending, [0, 1, 2, 3] },
        { FileSortColumn.FileName, SortDirection.Descending, [3, 2, 1, 0] },
        { FileSortColumn.SizeBytes, SortDirection.Ascending, [1, 2, 0, 3] },
        { FileSortColumn.SizeBytes, SortDirection.Descending, [3, 0, 2, 1] },
        { FileSortColumn.UploadedAt, SortDirection.Ascending, [0, 1, 2, 3] },
        { FileSortColumn.UploadedAt, SortDirection.Descending, [3, 2, 1, 0] },
    };
}

/// <summary>
/// The values of one seeded file ref.
/// </summary>
/// <param name="FileName">The stored name without the extension.</param>
/// <param name="Extension">The stored extension.</param>
/// <param name="SizeBytes">The size in bytes.</param>
/// <param name="CreatedAt">The upload time that the seed sets after the save.</param>
/// <param name="MachineId">The machine of the file, or <see langword="null"/>.</param>
public sealed record FileSeed(
    string FileName,
    string Extension,
    long SizeBytes,
    DateTime CreatedAt,
    Guid? MachineId = null,
    FilePurpose? Purpose = null,
    Guid? CompanyId = null
);
