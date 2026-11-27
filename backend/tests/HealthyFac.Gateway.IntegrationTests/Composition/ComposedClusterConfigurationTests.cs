using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Repository;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Feature(TestFeatures.Gateway)]
[Feature(TestFeatures.Drafts)]
public sealed class ComposedClusterConfigurationTests
{
    [Theory]
    [MemberData(nameof(CompositionTestData.ConfigurationFiles), MemberType = typeof(CompositionTestData))]
    public void Configuration_OfTheGateway_ListsEveryComposedCluster(string relativePath)
    {
        var text = File.ReadAllText(FindRepositoryFile(relativePath));

        var clusters = JsonNode.Parse(text)!["ReverseProxy"]!["Clusters"]!.AsObject();

        clusters.Select(cluster => cluster.Key).Should().Contain(CompositionTestData.ComposedClusterIds);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var path = Path.Combine(RepositoryRoot.Find().FullName, relativePath);

        return File.Exists(path) ? path : throw new FileNotFoundException(relativePath);
    }
}
