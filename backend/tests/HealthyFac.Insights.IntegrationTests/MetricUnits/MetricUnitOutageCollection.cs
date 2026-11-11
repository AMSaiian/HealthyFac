using HealthyFac.Insights.IntegrationTests.Database.Data;

namespace HealthyFac.Insights.IntegrationTests.MetricUnits;

/// <summary>
/// A collection of its own, because its test stops the Redis container.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MetricUnitOutageCollection : ICollectionFixture<InsightsFixture>
{
    public const string Name = "InsightsMetricUnitOutage";
}
