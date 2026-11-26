using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

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
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath)))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new FileNotFoundException(relativePath)
            : Path.Combine(directory.FullName, relativePath);
    }
}
