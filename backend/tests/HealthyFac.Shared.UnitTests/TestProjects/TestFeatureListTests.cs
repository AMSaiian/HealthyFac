using HealthyFac.Shared.UnitTests.TestProjects.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.TestProjects;

[Feature(TestFeatures.Conventions)]
public sealed class TestFeatureListTests
{
    [Fact]
    public void TestFeatures_AgainstFeatureFile_HaveTheSameNamesInTheSameOrder()
    {
        var fileNames = TestFeatureListTestData.ReadFeatureFileNames();

        var constants = TestFeatures.All;

        constants.Should().Equal(fileNames);
    }
}
