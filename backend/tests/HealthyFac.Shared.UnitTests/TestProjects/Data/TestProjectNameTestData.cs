namespace HealthyFac.Shared.UnitTests.TestProjects.Data;

public static class TestProjectNameTestData
{
    public const string SolutionFile = "HealthyFac.slnx";
    public const string TestsFolder = "tests";
    public const string HelperProjectFolder = "HealthyFac.Testing";

    public static readonly string[] Suffixes = ["UnitTests", "ArchitectureTests", "IntegrationTests", "ApiTests"];

    /// <summary>
    /// Walks up from the output folder of the test to the folder of the solution file and returns its <c>tests</c>
    /// folder.
    /// </summary>
    /// <returns>The path of <c>backend/tests</c>.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when no parent folder holds the solution file.</exception>
    public static string FindTestsDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFile)))
            {
                return Path.Combine(directory.FullName, TestsFolder);
            }
        }

        throw new DirectoryNotFoundException($"No parent folder of {AppContext.BaseDirectory} holds {SolutionFile}.");
    }
}
