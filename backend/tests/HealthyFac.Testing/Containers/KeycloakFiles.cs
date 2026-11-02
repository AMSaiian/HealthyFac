namespace HealthyFac.Testing.Containers;

/// <summary>
/// Finds the files of the <c>docker/keycloak</c> folder (the realm file and the user profile) for the Keycloak
/// tests, so every test project uses one search.
/// </summary>
public static class KeycloakFiles
{
    private static readonly string[] KeycloakDirectory = ["docker", "keycloak"];

    /// <summary>
    /// Finds a file in <c>docker/keycloak</c> by walking up from the test output until the file exists.
    /// </summary>
    /// <param name="relativePath">The path segments below <c>docker/keycloak</c>, for example
    /// <c>"import", "healthyfac-realm.json"</c>.</param>
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
}
