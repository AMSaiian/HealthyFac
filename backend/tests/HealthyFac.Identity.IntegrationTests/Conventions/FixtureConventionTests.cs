using HealthyFac.Testing.Conventions;

namespace HealthyFac.Identity.IntegrationTests.Conventions;

public sealed class FixtureConventionTests
{
    [Fact]
    public void ClassFixtures_InTestAssembly_BelongToOneTestClass()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindSharedClassFixtures(typeof(FixtureConventionTests).Assembly));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void CollectionFixtures_InTestAssembly_ResetBeforeEachTest()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindCollectionFixturesWithoutReset(typeof(FixtureConventionTests).Assembly));

        violations.Should().BeEmpty();
    }
}
