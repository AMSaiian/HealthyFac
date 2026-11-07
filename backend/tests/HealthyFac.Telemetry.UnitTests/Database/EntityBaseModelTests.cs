using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.UnitTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Telemetry.UnitTests.Database;

public sealed class EntityBaseModelTests
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    [Fact]
    public void Model_EveryEntityType_ImplementsIEntity()
    {
        var options = new DbContextOptionsBuilder<TelemetryDbContext>().UseNpgsql(UnusedConnectionString).Options;
        using var dbContext = new TelemetryDbContext(options);

        var entitiesWithoutBase = dbContext.GetService<IDesignTimeModel>()
            .Model.GetEntityTypes()
            .Where(entity => !typeof(IEntity).IsAssignableFrom(entity.ClrType))
            .Select(entity => entity.ClrType.Name)
            .Except(EntityBaseModelTestData.ExemptEntityNames)
            .ToList();

        entitiesWithoutBase.Should().BeEmpty();
    }
}
