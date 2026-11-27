using HealthyFac.Testing.Repository;

namespace HealthyFac.Shared.UnitTests.TestProjects.Data;

public static class TestProjectNameTestData
{
    private const string TestsPath = "backend/tests";

    public const string HelperProjectFolder = "HealthyFac.Testing";

    public static readonly string[] Suffixes = ["UnitTests", "ArchitectureTests", "IntegrationTests", "ApiTests"];

    /// <summary>
    /// Finds the repository root from the output folder of the test and returns its <c>backend/tests</c> folder.
    /// </summary>
    /// <returns>The path of <c>backend/tests</c>.</returns>
    public static string FindTestsDirectory()
        => Path.Combine(RepositoryRoot.Find().FullName, TestsPath);
}
