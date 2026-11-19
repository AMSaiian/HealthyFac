using HealthyFac.Insights.IntegrationTests.Cases.Data;

namespace HealthyFac.Insights.IntegrationTests.Cases;

[CollectionDefinition(Name)]
public sealed class SignalCaseCollection : ICollectionFixture<SignalCaseFixture>
{
    public const string Name = "InsightsSignalCases";
}
