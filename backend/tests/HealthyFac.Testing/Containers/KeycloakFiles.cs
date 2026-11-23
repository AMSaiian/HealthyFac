namespace HealthyFac.Testing.Containers;

/// <summary>
/// Finds the files of the <c>docker/keycloak</c> folder (the realm file and the user profile) for the Keycloak
/// tests, so every test project uses one search.
/// </summary>
public static class KeycloakFiles
{
    private static readonly string[] KeycloakDirectory = ["docker", "keycloak"];
    private static readonly string[] TestFileDirectory = ["backend", "tests", "HealthyFac.Testing", "Keycloak"];

    /// <summary>
    /// Finds a file in <c>docker/keycloak</c> by walking up from the test output until the file exists.
    /// </summary>
    /// <param name="relativePath">The path segments below <c>docker/keycloak</c>, for example
    /// <c>"realm", "healthyfac-realm.json"</c>.</param>
    /// <returns>The full path of the file.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when no folder above the test output holds the file.
    /// </exception>
    public static string Find(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. KeycloakDirectory, .. relativePath]);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"The file {Path.Combine(relativePath)} was not found in docker/keycloak above the test output.");
    }

    /// <summary>
    /// Finds a Keycloak file of the tests (the bootstrap file and the test tenant file) in
    /// <c>backend/tests/HealthyFac.Testing/Keycloak</c> by walking up from the test output until the file exists.
    /// </summary>
    /// <param name="fileName">The file name, for example <c>"healthyfac-test-bootstrap.json"</c>.</param>
    /// <returns>The full path of the file.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when no folder above the test output holds the file.
    /// </exception>
    public static string FindTestFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. TestFileDirectory, fileName]);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"The file {fileName} was not found in backend/tests/HealthyFac.Testing/Keycloak above the test output.");
    }
}
