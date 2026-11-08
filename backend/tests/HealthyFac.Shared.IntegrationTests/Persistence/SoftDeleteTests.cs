using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.IntegrationTests.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence;

public sealed class SoftDeleteTests(SoftDeleteFixture fixture) : IClassFixture<SoftDeleteFixture>
{
    private readonly SoftDeleteFixture _fixture = fixture;

    [Fact]
    public async Task Remove_SoftDeletableEntity_KeepsRowAndSetsDeletedFields()
    {
        var id = await ArrangeSoftDeletableAsync();

        await RemoveSoftDeletableAsync(id);

        await using var dbContext = _fixture.CreateDbContext();
        var rows = await dbContext.SoftDeletables.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(entity => entity.Id == id)
            .ToListAsync(TestContext.Current.CancellationToken);

        rows.Should().ContainSingle();
        rows[index: 0].DeletedAt.Should().Be(SoftDeleteTestData.Now.UtcDateTime);
        rows[index: 0].DeletedBy.Should().Be(SoftDeleteTestData.ActorId);
    }

    [Fact]
    public async Task Query_AfterRemove_HidesEntity()
    {
        var id = await ArrangeSoftDeletableAsync();

        await RemoveSoftDeletableAsync(id);

        await using var dbContext = _fixture.CreateDbContext();
        var visibleCount = await dbContext.SoftDeletables.CountAsync(
            entity => entity.Id == id, TestContext.Current.CancellationToken);

        visibleCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Remove_EntityWithoutSoftDelete_DeletesRow()
    {
        var entity = new PlainTestEntity { Name = SoftDeleteTestData.EntityName };
        await using (var arrange = _fixture.CreateDbContext())
        {
            arrange.Plains.Add(entity);
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var act = _fixture.CreateDbContext())
        {
            var tracked = await act.Plains.SingleAsync(
                plain => plain.Id == entity.Id, TestContext.Current.CancellationToken);
            act.Plains.Remove(tracked);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var rowCount = await dbContext.Plains.CountAsync(
            plain => plain.Id == entity.Id, TestContext.Current.CancellationToken);

        rowCount.Should().Be(expected: 0);
    }

    private async Task<Guid> ArrangeSoftDeletableAsync()
    {
        var entity = new SoftDeleteTestEntity { Name = SoftDeleteTestData.EntityName };
        await using var dbContext = _fixture.CreateDbContext();
        dbContext.SoftDeletables.Add(entity);
        await dbContext.SaveChangesAsync();

        return entity.Id;
    }

    private async Task RemoveSoftDeletableAsync(Guid id)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var tracked = await dbContext.SoftDeletables.SingleAsync(entity => entity.Id == id);
        dbContext.SoftDeletables.Remove(tracked);
        await dbContext.SaveChangesAsync();
    }
}
