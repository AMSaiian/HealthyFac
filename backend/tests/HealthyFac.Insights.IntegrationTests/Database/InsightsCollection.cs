using HealthyFac.Insights.IntegrationTests.Database.Data;

namespace HealthyFac.Insights.IntegrationTests.Database;

[CollectionDefinition(Name)]
public sealed class InsightsCollection : ICollectionFixture<InsightsFixture>
{
    public const string Name = "Insights";
}
