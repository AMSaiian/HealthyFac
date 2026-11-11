using HealthyFac.Insights.UnitTests.Database.Data;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Insights.UnitTests.Database;

public sealed class SoftDeleteModelTests
{
    [Fact]
    public void Model_SoftDeletableEntities_HaveQueryFilter()
    {
        var entitiesWithoutFilter = GetSoftDeletableEntityTypes()
            .Where(entity => entity.GetDeclaredQueryFilters()
                .All(filter => filter.Key != SoftDeleteConstants.QueryFilterName))
            .Select(entity => entity.DisplayName())
            .ToList();

        entitiesWithoutFilter.Should().BeEmpty();
    }

    [Fact]
    public void Model_UniqueIndexesOfSoftDeletableEntities_ExcludeDeletedRows()
    {
        var indexesWithoutFilter = GetSoftDeletableEntityTypes()
            .SelectMany(entity => entity.GetDeclaredIndexes())
            .Where(index => index.IsUnique)
            .Where(index => index.GetFilter()?.Contains(SoftDeleteConstants.NotDeletedFilter) != true)
            .Select(index => $"{index.DeclaringEntityType.DisplayName()}.{index.GetDatabaseName()}")
            .Except(SoftDeleteModelTestData.UnfilteredUniqueIndexes)
            .ToList();

        indexesWithoutFilter.Should().BeEmpty();
    }

    private static List<IEntityType> GetSoftDeletableEntityTypes() =>
        InsightsModelFactory.GetEntityTypes()
            .Where(entity => typeof(ISoftDeletable).IsAssignableFrom(entity.ClrType))
            .ToList();
}
