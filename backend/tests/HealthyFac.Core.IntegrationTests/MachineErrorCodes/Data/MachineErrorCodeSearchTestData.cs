using HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;

public static class MachineErrorCodeSearchTestData
{
    public const string LowerAlphaPrefix = "alpha";
    public const int PageSize = 2;
    public const int SecondPageOffset = 2;

    public static readonly DateTime FirstCreatedAt = new(
        year: 2026, month: 3, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static IReadOnlyList<MachineErrorCodeSeed> CreateSeeds() =>
    [
        new(Code: "Alpha-1", FirstCreatedAt, FirstCreatedAt.AddDays(value: 13)),
        new(Code: "alpha-2", FirstCreatedAt.AddDays(value: 1), FirstCreatedAt.AddDays(value: 11)),
        new(Code: "Beta-1", FirstCreatedAt.AddDays(value: 2), FirstCreatedAt.AddDays(value: 12)),
        new(Code: "Gamma-1", FirstCreatedAt.AddDays(value: 3), FirstCreatedAt.AddDays(value: 10)),
    ];

    public static SearchMachineErrorCodesRequest CreateSearch(
        Guid companyId,
        Guid machineId,
        MachineErrorCodeGridFilter? filter = null,
        GridSort<MachineErrorCodeSortColumn>? sort = null,
        int offset = 0,
        int limit = 25
    ) =>
        new()
        {
            CompanyId = companyId,
            MachineId = machineId,
            Offset = offset,
            Limit = limit,
            Sort = sort,
            Filter = filter ?? new MachineErrorCodeGridFilter(),
        };

    public static TheoryData<MachineErrorCodeSortColumn, SortDirection, int[]> SortCases =>
        new()
        {
            { MachineErrorCodeSortColumn.Code, SortDirection.Ascending, [0, 1, 2, 3] },
            { MachineErrorCodeSortColumn.Code, SortDirection.Descending, [3, 2, 1, 0] },
            { MachineErrorCodeSortColumn.CreatedAt, SortDirection.Descending, [3, 2, 1, 0] },
            { MachineErrorCodeSortColumn.UpdatedAt, SortDirection.Ascending, [3, 1, 2, 0] },
        };
}
