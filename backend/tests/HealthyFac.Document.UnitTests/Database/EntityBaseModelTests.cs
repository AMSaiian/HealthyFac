using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.UnitTests.Database.Data;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Document.UnitTests.Database;

public sealed class EntityBaseModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryEntityType_ImplementsIEntity()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new DocumentDbContext(options);

        var entitiesWithoutBase = dbContext.GetService<IDesignTimeModel>()
            .Model.GetEntityTypes()
            .Where(entity => !typeof(IEntity).IsAssignableFrom(entity.ClrType))
            .Select(entity => entity.ClrType.Name)
            .Except(EntityBaseModelTestData.ExemptEntityNames)
            .ToList();

        entitiesWithoutBase.Should().BeEmpty();
    }
}
