using HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;

public static class MachineErrorCodeSearchTestData
{
    public const string LowerAlphaPrefix = "alpha";
    public const int PageSize = 2;
    public const int SecondPageOffset = 2;

    public static readonly DateTime FirstCreatedAt = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<MachineErrorCodeSeed> CreateSeeds() =>
    [
        new("Alpha-1", FirstCreatedAt, FirstCreatedAt.AddDays(13)),
        new("alpha-2", FirstCreatedAt.AddDays(1), FirstCreatedAt.AddDays(11)),
        new("Beta-1", FirstCreatedAt.AddDays(2), FirstCreatedAt.AddDays(12)),
        new("Gamma-1", FirstCreatedAt.AddDays(3), FirstCreatedAt.AddDays(10)),
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
