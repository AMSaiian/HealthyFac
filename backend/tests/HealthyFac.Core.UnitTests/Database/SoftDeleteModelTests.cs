using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Core.UnitTests.Database;

public sealed class SoftDeleteModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_SoftDeletableEntities_HaveQueryFilter()
    {
        var entityTypes = GetSoftDeletableEntityTypes();

        var entitiesWithoutFilter = entityTypes
            .Where(entity => entity.GetDeclaredQueryFilters().Count == 0)
            .Select(entity => entity.DisplayName())
            .ToList();

        entityTypes.Should().NotBeEmpty();
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
            .ToList();

        indexesWithoutFilter.Should().BeEmpty();
    }

    private static List<IEntityType> GetSoftDeletableEntityTypes()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new CoreDbContext(options);

        return dbContext.GetService<IDesignTimeModel>()
            .Model.GetEntityTypes()
            .Where(entity => typeof(ISoftDeletable).IsAssignableFrom(entity.ClrType))
            .ToList();
    }
}
