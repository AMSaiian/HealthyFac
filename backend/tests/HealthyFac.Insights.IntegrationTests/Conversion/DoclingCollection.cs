namespace HealthyFac.Insights.IntegrationTests.Conversion;

/// <summary>
/// A collection of its own, because the container is large and starts slowly.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DoclingCollection : ICollectionFixture<DoclingFixture>
{
    public const string Name = "InsightsDocling";
}
