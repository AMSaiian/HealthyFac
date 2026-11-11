namespace HealthyFac.Insights.ApiTests.Hosting;

[CollectionDefinition(Name)]
public sealed class InsightsApiCollection : ICollectionFixture<InsightsApiFixture>
{
    public const string Name = "InsightsApi";
}
