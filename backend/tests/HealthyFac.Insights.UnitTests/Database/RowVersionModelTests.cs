using HealthyFac.Testing.Features;
using HealthyFac.Testing.Persistence;

namespace HealthyFac.Insights.UnitTests.Database;

[Feature(TestFeatures.Persistence)]
public sealed class RowVersionModelTests
{
    [Fact]
    public void Model_EveryRowVersion_IsVersionedPropertyOnXmin()
    {
        var entityTypes = InsightsModelFactory.GetEntityTypes();

        var wrongRowVersions = RowVersionModelChecks.FindWrongRowVersions(entityTypes);

        wrongRowVersions.Should().BeEmpty();
    }
}
