using HealthyFac.Testing.Repository;

namespace HealthyFac.Shared.UnitTests.TestProjects.Data;

public static class TestProjectNameTestData
{
    private const string TestsPath = "backend/tests";

    public const string HelperProjectFolder = "HealthyFac.Testing";

    /// <summary>The test output folder of the <c>.gitignore</c> pattern <c>[Tt]est[Rr]esult*/</c>.</summary>
    public const string TestOutputFolder = "TestResults";

    public static readonly string[] Suffixes = ["UnitTests", "ArchitectureTests", "IntegrationTests", "ApiTests"];

    /// <summary>
    /// Lists the folders of a tests folder whose name does not end in a known test project suffix. The helper project
    /// and the test output folder never count.
    /// </summary>
    /// <param name="testsDirectory">The path of the tests folder.</param>
    /// <returns>The names of the misnamed folders.</returns>
    public static List<string> FindMisnamedFolders(string testsDirectory) =>
    [
        .. Directory.GetDirectories(testsDirectory)
            .Select(path => Path.GetFileName(path))
            .Where(name => name is not (HelperProjectFolder or TestOutputFolder))
            .Where(name => !Suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal))),
    ];

    /// <summary>
    /// Finds the repository root from the output folder of the test and returns its <c>backend/tests</c> folder.
    /// </summary>
    /// <returns>The path of <c>backend/tests</c>.</returns>
    public static string FindTestsDirectory()
        => Path.Combine(RepositoryRoot.Find().FullName, TestsPath);
}
