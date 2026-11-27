using HealthyFac.Shared.UnitTests.TestProjects.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.TestProjects;

[Feature(TestFeatures.Conventions)]
public sealed class TestProjectNameTests
{
    [Fact]
    public void TestProjectFolders_InBackendTests_EndInKnownSuffix()
    {
        var misnamed = TestProjectNameTestData.FindMisnamedFolders(TestProjectNameTestData.FindTestsDirectory());

        misnamed.Should().BeEmpty();
    }

    [Fact]
    public void FindMisnamedFolders_WithAnEmptyTestOutputFolder_SkipsIt()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, TestProjectNameTestData.TestOutputFolder));
            Directory.CreateDirectory(Path.Combine(root.FullName, path2: "HealthyFac.Core.UnitTests"));

            var misnamed = TestProjectNameTestData.FindMisnamedFolders(root.FullName);

            misnamed.Should().BeEmpty();
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindMisnamedFolders_WithAFolderOfAnotherSuffix_ListsIt()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, TestProjectNameTestData.TestOutputFolder));
            Directory.CreateDirectory(Path.Combine(root.FullName, path2: "HealthyFac.Core.Tests"));

            var misnamed = TestProjectNameTestData.FindMisnamedFolders(root.FullName);

            misnamed.Should().Equal("HealthyFac.Core.Tests");
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
