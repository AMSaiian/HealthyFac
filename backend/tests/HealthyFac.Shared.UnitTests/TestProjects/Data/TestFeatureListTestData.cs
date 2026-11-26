using System.Text.Json;
using HealthyFac.Testing.Repository;

namespace HealthyFac.Shared.UnitTests.TestProjects.Data;

public static class TestFeatureListTestData
{
    private const string FeatureFileName = "test-features.json";

    public static IReadOnlyList<string> ReadFeatureFileNames()
    {
        var folder = RepositoryRoot.Find();

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder.FullName, FeatureFileName)));

        return
        [
            .. document.RootElement.GetProperty("features")
                .EnumerateArray()
                .Select(feature => feature.GetProperty("name").GetString()!),
        ];
    }
}
