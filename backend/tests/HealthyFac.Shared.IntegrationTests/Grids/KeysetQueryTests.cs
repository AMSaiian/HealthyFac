using System.Text.Json;
using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.IntegrationTests.Grids.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Grids;

[Collection(GridDatabaseCollection.Name)]
public sealed class KeysetQueryTests(GridDatabaseFixture fixture)
{
    private readonly GridDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(KeysetTestData.Cases), MemberType = typeof(KeysetTestData))]
    public async Task ToKeysetResponseAsync_ReadingEveryPage_ReturnsEveryRowOnceInOrder(
        string columnName,
        bool isDescending
    )
    {
        await using var dbContext = _fixture.CreateDbContext();
        var allRows = await ReadAllAsync(dbContext);
        var expectedIds = KeysetTestData.Order(allRows, columnName, isDescending).Select(item => item.Id).ToList();

        var actualIds = new List<Guid>();
        KeysetPosition? after = null;

        do
        {
            var page = await Items(dbContext)
                .ToKeysetResponseAsync(
                    KeysetTestData.SortOf(columnName, isDescending),
                    KeysetTestSortMap.Instance,
                    KeysetTestData.PageLimit,
                    after,
                    TestContext.Current.CancellationToken);

            actualIds.AddRange(page.Items.Select(item => item.Id));
            after = page.NextPosition;
        }
        while (after is not null && actualIds.Count <= expectedIds.Count);

        actualIds.Should().Equal(expectedIds);
    }

    [Fact]
    public async Task ToKeysetResponseAsync_AfterTheLastRowChanged_StartsAfterTheStoredPosition()
    {
        var sort = KeysetTestData.SortOf(columnName: "rank", isDescending: false);
        await using var dbContext = _fixture.CreateDbContext();
        var snapshot = await ReadAllAsync(dbContext);

        var pageOne = await Items(dbContext)
            .ToKeysetResponseAsync(
                sort,
                KeysetTestSortMap.Instance,
                KeysetTestData.PageLimit,
                after: null,
                TestContext.Current.CancellationToken);

        var lastRow = pageOne.Items[^1];

        var expectedFirstId = KeysetTestData
            .Order(snapshot, columnName: "rank", isDescending: false)
            .SkipWhile(item => item.Id != lastRow.Id)
            .Skip(count: 1)
            .First()
            .Id;

        const int movedRank = 99;

        var entity = await dbContext.Rows.SingleAsync(
            row => row.Id == lastRow.Id, TestContext.Current.CancellationToken);

        entity.Number = movedRank;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        try
        {
            await using var readContext = _fixture.CreateDbContext();

            var pageTwo = await Items(readContext)
                .ToKeysetResponseAsync(
                    sort,
                    KeysetTestSortMap.Instance,
                    KeysetTestData.PageLimit,
                    pageOne.NextPosition,
                    TestContext.Current.CancellationToken);

            pageTwo.Items[0].Id.Should().Be(expectedFirstId);
        }
        finally
        {
            entity.Number = lastRow.Rank;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    [Theory]
    [MemberData(nameof(KeysetTestData.InvalidPositionCases), MemberType = typeof(KeysetTestData))]
    public void After_WithSortValueThatDoesNotFit_ThrowsValidationException(string columnName, string json)
    {
        var position = new KeysetPosition { SortValue = JsonDocument.Parse(json).RootElement, Id = Guid.NewGuid() };
        var key = KeysetTestSortMap.Instance[KeysetSortColumn.FromValue(columnName)];
        using var dbContext = _fixture.CreateDbContext();

        var act = () => key.After(Items(dbContext), SortDirection.Ascending, position);

        act.Should()
            .Throw<AppValidationException>()
            .Which.Errors.Should()
            .ContainSingle()
            .Which.Should()
            .Match<ValidationFailure>(failure =>
                failure.PropertyName == GridErrors.Validation.PositionSortValueKey
                && failure.ErrorMessage == GridErrors.Validation.PositionSortValueInvalid);
    }

    private static IQueryable<KeysetTestItem> Items(GridTestDbContext dbContext) =>
        dbContext.Rows
            .Where(row => row.Group == GridTestData.KeysetGroup)
            .Select(row => new KeysetTestItem
            {
                Id = row.Id,
                Rank = row.Number,
                Big = row.Big,
                Amount = row.Amount,
                Ratio = row.Ratio,
                At = row.At,
                Name = row.Text,
            });

    private static async Task<List<KeysetTestItem>> ReadAllAsync(GridTestDbContext dbContext) =>
        await Items(dbContext).ToListAsync(TestContext.Current.CancellationToken);
}
