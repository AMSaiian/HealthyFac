using HealthyFac.Shared.Application.Grids;
using HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

public static class StatusDefinitionSearchTestData
{
    public const string LowerAlphaPrefix = "alpha";
    public const int PageSize = 2;
    public const int SecondPageOffset = 2;

    public static readonly IReadOnlyList<StatusSeverity> SeveritiesInAscendingOrder =
        [StatusSeverity.Info, StatusSeverity.Info, StatusSeverity.Warning, StatusSeverity.Error];

    public static readonly DateTime FirstCreatedAt = new(
        year: 2026, month: 3, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static IReadOnlyList<StatusDefinitionSeed> CreateSeeds() =>
    [
        new(Code: "Alpha-1", FirstCreatedAt, FirstCreatedAt.AddDays(value: 13), StatusSeverity.Warning),
        new(Code: "alpha-2", FirstCreatedAt.AddDays(value: 1), FirstCreatedAt.AddDays(value: 11), StatusSeverity.Info),
        new(Code: "Beta-1", FirstCreatedAt.AddDays(value: 2), FirstCreatedAt.AddDays(value: 12), StatusSeverity.Error),
        new(Code: "Gamma-1", FirstCreatedAt.AddDays(value: 3), FirstCreatedAt.AddDays(value: 10), StatusSeverity.Info),
    ];

    public static SearchStatusDefinitionsRequest CreateSearch(
        Guid companyId,
        Guid machineId,
        StatusDefinitionGridFilter? filter = null,
        GridSort<StatusDefinitionSortColumn>? sort = null,
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
            Filter = filter ?? new StatusDefinitionGridFilter(),
        };

    public static TheoryData<StatusDefinitionSortColumn, SortDirection, int[]> SortCases =>
        new()
        {
            { StatusDefinitionSortColumn.Code, SortDirection.Ascending, [0, 1, 2, 3] },
            { StatusDefinitionSortColumn.Code, SortDirection.Descending, [3, 2, 1, 0] },
            { StatusDefinitionSortColumn.CreatedAt, SortDirection.Descending, [3, 2, 1, 0] },
            { StatusDefinitionSortColumn.UpdatedAt, SortDirection.Ascending, [3, 1, 2, 0] },
        };
}
