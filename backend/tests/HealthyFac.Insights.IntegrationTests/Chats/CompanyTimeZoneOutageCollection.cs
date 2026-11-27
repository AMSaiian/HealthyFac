using HealthyFac.Insights.IntegrationTests.Database.Data;

namespace HealthyFac.Insights.IntegrationTests.Chats;

/// <summary>
/// A collection of its own, because its test stops the Redis container and the reset of the next test needs it.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CompanyTimeZoneOutageCollection : ICollectionFixture<InsightsFixture>
{
    public const string Name = "InsightsCompanyTimeZoneOutage";
}
