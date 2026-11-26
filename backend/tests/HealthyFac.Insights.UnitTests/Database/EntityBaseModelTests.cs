using HealthyFac.Insights.UnitTests.Database.Data;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Database;

[Feature(TestFeatures.Persistence)]
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
