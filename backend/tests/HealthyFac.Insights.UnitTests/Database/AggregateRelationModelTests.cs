using HealthyFac.Testing.Features;
using HealthyFac.Testing.Persistence;

namespace HealthyFac.Insights.UnitTests.Database;

[Feature(TestFeatures.Persistence)]
public sealed class AggregateRelationModelTests
{
    [Fact]
    public void Model_EveryRelation_FollowsAggregateRule()
    {
        var entityTypes = InsightsModelFactory.GetEntityTypes();

        var wrongRelations = AggregateRelationModelChecks.FindWrongRelations(entityTypes);

        string.Join(Environment.NewLine, wrongRelations).Should().BeEmpty();
    }
}
