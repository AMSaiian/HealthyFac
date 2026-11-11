using HealthyFac.Insights.UnitTests.Database.Data;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.UnitTests.Database;

public sealed class EntityBaseModelTests
{
    [Fact]
    public void Model_EveryEntityType_ImplementsIEntity()
    {
        var entitiesWithoutBase = InsightsModelFactory.GetEntityTypes()
            .Where(entity => !typeof(IEntity).IsAssignableFrom(entity.ClrType))
            .Select(entity => entity.ClrType.Name)
            .Except(EntityBaseModelTestData.ExemptEntityNames)
            .ToList();

        entitiesWithoutBase.Should().BeEmpty();
    }
}
