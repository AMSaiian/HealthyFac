using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
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

    [Fact]
    public async Task Remove_ParentWithLoadedChildren_SoftDeletesTheChildrenToo()
    {
        var parentId = await ArrangeParentAsync(childCount: 2);

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.Include(entity => entity.Children)
                .SingleAsync(entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var children = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        children.Should().HaveCount(expected: 2);
        children.Should().OnlyContain(child => child.DeletedAt == SoftDeleteTestData.Now.UtcDateTime);
        children.Should().OnlyContain(child => child.DeletedBy == SoftDeleteTestData.ActorId);
    }

    [Fact]
    public async Task Save_ChildRemovedFromTheCollectionOfItsParent_SoftDeletesTheOrphanOnly()
    {
        var parentId = await ArrangeParentAsync(childCount: 2);

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.Include(entity => entity.Children)
                .SingleAsync(entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            parent.Children.RemoveAt(index: 0);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var rows = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        rows.Should().HaveCount(expected: 2);
        rows.Count(child => child.DeletedAt is not null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task ExecuteDelete_ParentWithChildren_RemovesTheChildren()
    {
        var parentId = await ArrangeParentAsync(childCount: 1);
        await using (var act = _fixture.CreateDbContext())
        {
            await act.Parents.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(entity => entity.Id == parentId)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var rows = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_ParentWithUnloadedChildren_SoftDeletesChildrenAndGrandchildren()
    {
        var parentId = await ArrangeTreeAsync();

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var children = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        var childIds = children.Select(child => child.Id).ToList();
        var grandchildren = await dbContext.Grandchildren.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(grandchild => childIds.Contains(grandchild.ChildId))
            .ToListAsync(TestContext.Current.CancellationToken);

        children.Should().HaveCount(expected: 2);
        children.Should().OnlyContain(child => child.DeletedAt == SoftDeleteTestData.Now.UtcDateTime);
        children.Should().OnlyContain(child => child.DeletedBy == SoftDeleteTestData.ActorId);

        grandchildren.Should().HaveCount(expected: 2);
        grandchildren.Should().OnlyContain(grandchild => grandchild.DeletedAt == SoftDeleteTestData.Now.UtcDateTime);
        grandchildren.Should().OnlyContain(grandchild => grandchild.DeletedBy == SoftDeleteTestData.ActorId);
    }

    [Fact]
    public async Task Remove_ParentWithDeletedChild_KeepsItsDeleteTime()
    {
        var parentId = await ArrangeParentAsync(childCount: 2);
        await using (var first = _fixture.CreateDbContext())
        {
            var child = await first.Children.FirstAsync(
                entity => entity.ParentId == parentId, TestContext.Current.CancellationToken);
            first.Children.Remove(child);
            await first.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        _fixture.TimeProvider.Advance(TimeSpan.FromHours(value: 1));
        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        _fixture.ResetClock();
        await using var dbContext = _fixture.CreateDbContext();
        var children = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var secondTime = SoftDeleteTestData.Now.UtcDateTime + TimeSpan.FromHours(value: 1);
        children.Select(child => child.DeletedAt)
            .Should()
            .BeEquivalentTo(
                new DateTime?[] { SoftDeleteTestData.Now.UtcDateTime, secondTime });
    }

    [Fact]
    public async Task Remove_ParentAfterReassigningAChildByForeignKey_KeepsTheMovedChild()
    {
        var firstParentId = await ArrangeParentAsync(childCount: 2);
        var secondParentId = await ArrangeParentAsync(childCount: 0);

        Guid movedChildId;
        await using (var act = _fixture.CreateDbContext())
        {
            var parents = await act.Parents.Include(entity => entity.Children)
                .Where(entity => entity.Id == firstParentId || entity.Id == secondParentId)
                .ToListAsync(TestContext.Current.CancellationToken);
            var firstParent = parents.Single(entity => entity.Id == firstParentId);
            var movedChild = firstParent.Children[index: 0];
            movedChildId = movedChild.Id;
            act.Entry(movedChild).Property(nameof(SoftDeleteChildEntity.ParentId)).CurrentValue = secondParentId;
            act.Parents.Remove(firstParent);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var children = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == firstParentId || child.ParentId == secondParentId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var moved = children.Single(child => child.Id == movedChildId);
        var others = children.Where(child => child.Id != movedChildId).ToList();

        moved.ParentId.Should().Be(secondParentId);
        moved.DeletedAt.Should().BeNull();

        others.Should().ContainSingle();
        others[index: 0].DeletedAt.Should().Be(SoftDeleteTestData.Now.UtcDateTime);
    }

    [Fact]
    public async Task Remove_Parent_LeavesRestrictReferenceAndPlainChildUnchanged()
    {
        var parentId = await ArrangeParentAsync(childCount: 0);
        var reference = new SoftDeleteReferenceEntity { Name = "reference" };
        await using (var arrange = _fixture.CreateDbContext())
        {
            var parent = await arrange.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            parent.PlainChildren.Add(new PlainChildEntity { Name = "plain" });
            arrange.References.Add(reference);
            arrange.Entry(reference).Property(entity => entity.ParentId).CurrentValue = parentId;
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var referenceRow = await dbContext.References.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .SingleAsync(entity => entity.Id == reference.Id, TestContext.Current.CancellationToken);
        var plainCount = await dbContext.PlainChildren.CountAsync(
            child => child.ParentId == parentId, TestContext.Current.CancellationToken);

        referenceRow.DeletedAt.Should().BeNull();
        plainCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task Save_AfterRolledBackAttempt_SoftDeletesTheChildrenAgain()
    {
        var parentId = await ArrangeParentAsync(childCount: 2);

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            await using (var attempt = await act.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
            {
                await act.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestContext.Current.CancellationToken);
                await attempt.RollbackAsync(TestContext.Current.CancellationToken);
            }

            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var parentRow = await ReadParentAsync(dbContext, parentId);
        var children = await ReadChildrenAsync(dbContext, parentId);

        parentRow.DeletedAt.Should().Be(SoftDeleteTestData.Now.UtcDateTime);
        children.Should().HaveCount(expected: 2);
        children.Should().OnlyContain(child => child.DeletedAt == SoftDeleteTestData.Now.UtcDateTime);
    }

    [Fact]
    public async Task Save_InsideOpenTransactionThatRollsBack_ChangesNoRow()
    {
        var parentId = await ArrangeParentAsync(childCount: 2);

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            await using var attempt = await act.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            await act.SaveChangesAsync(TestContext.Current.CancellationToken);
            await attempt.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var parentRow = await ReadParentAsync(dbContext, parentId);
        var children = await ReadChildrenAsync(dbContext, parentId);

        parentRow.DeletedAt.Should().BeNull();
        children.Should().OnlyContain(child => child.DeletedAt == null);
    }

    [Fact]
    public async Task SaveChanges_ParentWithUnloadedChildren_SoftDeletesChildrenAndGrandchildren()
    {
        var parentId = await ArrangeTreeAsync();

        await using (var act = _fixture.CreateDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            act.SaveChanges();
        }

        await using var dbContext = _fixture.CreateDbContext();
        var children = await dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);
        var childIds = children.Select(child => child.Id).ToList();
        var grandchildren = await dbContext.Grandchildren.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(grandchild => childIds.Contains(grandchild.ChildId))
            .ToListAsync(TestContext.Current.CancellationToken);

        children.Should().HaveCount(expected: 2);
        children.Should().OnlyContain(child => child.DeletedAt == SoftDeleteTestData.Now.UtcDateTime);
        children.Should().OnlyContain(child => child.DeletedBy == SoftDeleteTestData.ActorId);

        grandchildren.Should().HaveCount(expected: 2);
        grandchildren.Should().OnlyContain(grandchild => grandchild.DeletedAt == SoftDeleteTestData.Now.UtcDateTime);
        grandchildren.Should().OnlyContain(grandchild => grandchild.DeletedBy == SoftDeleteTestData.ActorId);
    }

    [Fact]
    public async Task SaveChanges_WithoutSaveTransaction_ThrowsAndChangesNoRow()
    {
        var parentId = await ArrangeParentAsync(childCount: 2);

        await using (var act = _fixture.CreateUnguardedDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            var save = () => act.SaveChanges();
            save.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(SoftDeleteCascadeErrors.NoOpenTransaction().Message);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var parentRow = await ReadParentAsync(dbContext, parentId);
        var children = await ReadChildrenAsync(dbContext, parentId);

        parentRow.DeletedAt.Should().BeNull();
        children.Should().OnlyContain(child => child.DeletedAt == null);
    }

    [Fact]
    public async Task Save_WithoutSaveTransaction_ThrowsAndChangesNoRow()
    {
        var parentId = await ArrangeParentAsync(childCount: 1);

        await using (var act = _fixture.CreateUnguardedDbContext())
        {
            var parent = await act.Parents.SingleAsync(
                entity => entity.Id == parentId, TestContext.Current.CancellationToken);
            act.Parents.Remove(parent);
            var save = async () => await act.SaveChangesAsync(TestContext.Current.CancellationToken);
            await save.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage(SoftDeleteCascadeErrors.NoOpenTransaction().Message);
        }

        await using var dbContext = _fixture.CreateDbContext();
        var parentRow = await ReadParentAsync(dbContext, parentId);

        parentRow.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Remove_ParentOfCompositeKeyChild_Throws()
    {
        var parent = new CompositeKeyParentEntity { Name = SoftDeleteTestData.EntityName };
        parent.Children.Add(new CompositeKeyChildEntity { Position = 1 });
        await using (var arrange = _fixture.CreateDbContext())
        {
            arrange.CompositeParents.Add(parent);
            await arrange.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var act = _fixture.CreateDbContext();
        var tracked = await act.CompositeParents.SingleAsync(
            entity => entity.Id == parent.Id, TestContext.Current.CancellationToken);
        act.CompositeParents.Remove(tracked);
        var save = async () => await act.SaveChangesAsync(TestContext.Current.CancellationToken);

        await save.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(SoftDeleteCascadeErrors.UnsupportedKey(relation: "CompositeKeyChildEntity.ParentId").Message);
    }

    private static Task<SoftDeleteParentEntity> ReadParentAsync(SoftDeleteDbContext dbContext, Guid parentId) =>
        dbContext.Parents.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .SingleAsync(entity => entity.Id == parentId, TestContext.Current.CancellationToken);

    private static Task<List<SoftDeleteChildEntity>> ReadChildrenAsync(SoftDeleteDbContext dbContext, Guid parentId) =>
        dbContext.Children.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(child => child.ParentId == parentId)
            .ToListAsync(TestContext.Current.CancellationToken);

    private async Task<Guid> ArrangeTreeAsync()
    {
        var parent = new SoftDeleteParentEntity { Name = SoftDeleteTestData.EntityName };
        foreach (var index in Enumerable.Range(start: 0, count: 2))
        {
            var child = new SoftDeleteChildEntity { Name = $"child {index}" };
            child.Grandchildren.Add(new SoftDeleteGrandchildEntity { Name = $"grandchild {index}" });
            parent.Children.Add(child);
        }

        await using var dbContext = _fixture.CreateDbContext();
        dbContext.Parents.Add(parent);
        await dbContext.SaveChangesAsync();

        return parent.Id;
    }

    private async Task<Guid> ArrangeParentAsync(int childCount)
    {
        var parent = new SoftDeleteParentEntity { Name = SoftDeleteTestData.EntityName };
        parent.Children.AddRange(
            Enumerable.Range(start: 0, childCount)
                .Select(index => new SoftDeleteChildEntity { Name = $"child {index}" }));
        await using var dbContext = _fixture.CreateDbContext();
        dbContext.Parents.Add(parent);
        await dbContext.SaveChangesAsync();

        return parent.Id;
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
