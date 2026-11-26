using HealthyFac.Shared.UnitTests.TestProjects.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.TestProjects;

[Feature(TestFeatures.Conventions)]
public sealed class TestProjectNameTests
{
    [Fact]
    public void TestProjectFolders_InBackendTests_EndInKnownSuffix()
    {
        var misnamed = Directory.GetDirectories(TestProjectNameTestData.FindTestsDirectory())
            .Select(path => Path.GetFileName(path))
            .Where(name => name != TestProjectNameTestData.HelperProjectFolder)
            .Where(name =>
                !TestProjectNameTestData.Suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal)))
            .ToList();

        misnamed.Should().BeEmpty();
    }
}
