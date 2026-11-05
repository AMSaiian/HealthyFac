using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.IntegrationTests.Grids.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Grids;

public sealed class GridQueryTests(GridDatabaseFixture fixture) : IClassFixture<GridDatabaseFixture>
{
    private readonly GridDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(GridTestData.SortCases), MemberType = typeof(GridTestData))]
    public async Task ApplySort_ByEachKeyType_OrdersRows(string columnName, bool isDescending)
    {
        var sort = new GridSort<GridSortColumn>
        {
            Column = GridSortColumn.FromValue(columnName),
            Direction = isDescending ? SortDirection.Descending : SortDirection.Ascending,
        };
        var expectedIds = ExpectedOrder(GridTestData.SortGroup, columnName, isDescending);
        await using var dbContext = _fixture.CreateDbContext();

        var actualIds = await dbContext.Rows
            .Where(row => row.Group == GridTestData.SortGroup)
            .ApplySort(sort, GridTestSortMap.Instance)
            .Select(row => row.Id)
            .ToListAsync();

        actualIds.Should().Equal(expectedIds);
    }

    [Fact]
    public async Task ApplySort_WithEqualKeys_PagesWithoutGapsOrDuplicates()
    {
        var sort = new GridSort<GridSortColumn> { Column = GridSortColumn.At, Direction = SortDirection.Ascending };
        await using var dbContext = _fixture.CreateDbContext();
        var query = dbContext.Rows
            .Where(row => row.Group == GridTestData.PagingGroup)
            .ApplySort(sort, GridTestSortMap.Instance);

        var pages = new List<GridResponse<Guid>>();
        for (var offset = 0; offset < GridTestData.PagedRowCount; offset += GridTestData.PageSize)
        {
            pages.Add(
                await query.ToGridResponseAsync(
                    offset, GridTestData.PageSize, row => row.Id, default(CancellationToken)));
        }

        var ids = pages.SelectMany(page => page.Items).ToList();

        ids.Should().OnlyHaveUniqueItems().And.HaveCount(GridTestData.PagedRowCount);
        pages.Should().OnlyContain(page => page.TotalCount == GridTestData.PagingRowCount);
    }

    [Fact]
    public async Task ApplySort_WithoutSort_OrdersById()
    {
        var expectedIds = _fixture.Rows
            .Where(row => row.Group == GridTestData.SortGroup)
            .Select(row => row.Id)
            .Order(GridTestData.GuidBytewise)
            .ToList();
        await using var dbContext = _fixture.CreateDbContext();

        var actualIds = await dbContext.Rows
            .Where(row => row.Group == GridTestData.SortGroup)
            .ApplySort(null, GridTestSortMap.Instance)
            .Select(row => row.Id)
            .ToListAsync();

        actualIds.Should().Equal(expectedIds);
    }

    [Fact]
    public async Task TextFilter_IgnoresCase()
    {
        var filter = new GridTestFilter { Text = new TextFilter { Value = GridTestData.PrefixAb } };
        await using var dbContext = _fixture.CreateDbContext();

        var texts = await dbContext.Rows
            .Where(row => row.Group == GridTestData.TextGroup)
            .ApplyFilter(filter)
            .Select(row => row.Text)
            .ToListAsync();

        texts.Should().BeEquivalentTo("ABC", "abd");
    }

    [Fact]
    public async Task TextFilter_WithLikeCharacters_MatchesLiterally()
    {
        var filter = new GridTestFilter { Text = new TextFilter { Value = GridTestData.PercentPrefix } };
        await using var dbContext = _fixture.CreateDbContext();

        var texts = await dbContext.Rows
            .Where(row => row.Group == GridTestData.TextGroup)
            .ApplyFilter(filter)
            .Select(row => row.Text)
            .ToListAsync();

        texts.Should().BeEquivalentTo("50% off");
    }

    [Theory]
    [MemberData(nameof(GridTestData.DateRangeCases), MemberType = typeof(GridTestData))]
    public async Task DateRangeFilter_WithOpenBounds_FiltersInclusiveFromExclusiveTo(int? fromIndex, int? toIndex)
    {
        var range = new DateRangeFilter { From = AtIndex(fromIndex), To = AtIndex(toIndex) };
        var expectedIds = _fixture.Rows
            .Where(row => row.Group == GridTestData.DatesGroup)
            .Where(row => (range.From is null || row.At >= range.From) && (range.To is null || row.At < range.To))
            .Select(row => row.Id)
            .Order(GridTestData.GuidBytewise)
            .ToList();
        await using var dbContext = _fixture.CreateDbContext();

        var actualIds = await dbContext.Rows
            .Where(row => row.Group == GridTestData.DatesGroup)
            .ApplyFilter(new GridTestFilter { At = range })
            .ApplySort(null, GridTestSortMap.Instance)
            .Select(row => row.Id)
            .ToListAsync();

        actualIds.Should().Equal(expectedIds);
    }

    [Fact]
    public async Task DateRangeFilter_OnNullableColumn_ExcludesRowsWithoutValue()
    {
        var range = new DateRangeFilter { From = AtIndex(0) };
        await using var dbContext = _fixture.CreateDbContext();

        var count = await dbContext.Rows
            .Where(row => row.Group == GridTestData.DatesGroup)
            .ApplyFilter(new GridTestFilter { OptionalAt = range })
            .CountAsync();

        count.Should().Be(GridTestData.DatesRowCount / 2);
    }

    [Fact]
    public async Task ToGridResponseAsync_AfterFilter_CountsFilteredRows()
    {
        var filter = new GridTestFilter { Text = new TextFilter { Value = GridTestData.PrefixAb } };
        await using var dbContext = _fixture.CreateDbContext();

        var response = await dbContext.Rows
            .Where(row => row.Group == GridTestData.TextGroup)
            .ApplyFilter(filter)
            .ApplySort(null, GridTestSortMap.Instance)
            .ToGridResponseAsync(0, 1, row => row.Text, default(CancellationToken));

        response.TotalCount.Should().Be(2);
        response.Items.Should().HaveCount(1);
    }

    private static DateTime? AtIndex(int? index) =>
        index is null ? null : GridTestData.DatesBase + GridTestData.DatesStep * index.Value;

    private List<Guid> ExpectedOrder(string group, string columnName, bool isDescending)
    {
        var direction = isDescending ? -1 : 1;
        var rows = _fixture.Rows.Where(row => row.Group == group).ToList();

        rows.Sort((left, right) =>
        {
            var byKey = direction * CompareKey(columnName, left, right);

            return byKey != 0 ? byKey : GridTestData.GuidBytewise.Compare(left.Id, right.Id);
        });

        return rows.Select(row => row.Id).ToList();
    }

    private static int CompareKey(string columnName, GridTestEntity left, GridTestEntity right) =>
        columnName switch
        {
            "text" => string.CompareOrdinal(left.Text, right.Text),
            "number" => left.Number.CompareTo(right.Number),
            "amount" => left.Amount.CompareTo(right.Amount),
            "at" => left.At.CompareTo(right.At),
            _ => GridTestData.GuidBytewise.Compare(left.Key, right.Key),
        };
}
