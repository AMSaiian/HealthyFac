namespace HealthyFac.Testing.Seeding;

/// <summary>
/// Finds the committed seed data file <c>backend/seed/seed.Development.json</c> for the seed tests, so every test
/// project uses one search.
/// </summary>
public static class SeedDataFile
{
    private static readonly string[] SeedFileSegments = ["seed", "seed.Development.json"];

    /// <summary>
    /// Finds the file by walking up from the test output until a <c>seed</c> folder holds it.
    /// </summary>
    /// <returns>The full path of the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no folder above the test output holds the file.</exception>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. SeedFileSegments]);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"The file {Path.Combine(SeedFileSegments)} was not found above the test output.");
    }
}
