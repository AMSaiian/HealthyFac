using HealthyFac.Shared.UnitTests.TestProjects.Data;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Repository;

namespace HealthyFac.Shared.UnitTests.TestProjects;

[Feature(TestFeatures.Conventions)]
public sealed class TestFeatureListTests
{
    private const string PendingFilePath = "backend/tests/feature-audit-pending.txt";

    [Fact]
    public void TestFeatures_AgainstFeatureFile_HaveTheSameNamesInTheSameOrder()
    {
        var fileNames = TestFeatureListTestData.ReadFeatureFileNames();

        var constants = TestFeatures.All;

        constants.Should().Equal(fileNames);
    }

    [Fact]
    public void FeatureAuditPendingList_OfTheRepository_DoesNotExist()
    {
        var path = Path.Combine(RepositoryRoot.Find().FullName, PendingFilePath);

        File.Exists(path).Should().BeFalse("every test project passes the feature check");
    }
}
