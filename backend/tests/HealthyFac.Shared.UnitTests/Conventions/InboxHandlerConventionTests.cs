using HealthyFac.Shared.UnitTests.Conventions.Data;
using HealthyFac.Testing.Conventions;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Conventions;

[Feature(TestFeatures.Conventions)]
public sealed class InboxHandlerConventionTests
{
    [Fact]
    public void FindInboxHandlerTransactions_WithPlantedTypes_ReportsOnlyTheInboxHandler()
    {
        var violations = ConventionChecks.FindInboxHandlerTransactions(
        [
            typeof(PlantedInboxHandler),
            typeof(PlantedRequestHandler),
            typeof(PlantedCleanInboxHandler),
        ]);

        violations.Should().Equal($"{typeof(PlantedInboxHandler).FullName}: calls RunInTransactionAsync");
    }
}
