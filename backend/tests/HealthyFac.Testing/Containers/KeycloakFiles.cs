using HealthyFac.Testing.Repository;

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
    /// Finds a file in <c>docker/keycloak</c> under the repository root.
    /// </summary>
    /// <param name="relativePath">The path segments below <c>docker/keycloak</c>, for example
    /// <c>"realm", "healthyfac-realm.json"</c>.</param>
    /// <returns>The full path of the file.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the repository root holds no such file.
    /// </exception>
    public static string Find(params string[] relativePath)
    {
        var candidate = Path.Combine([RepositoryRoot.Find().FullName, .. KeycloakDirectory, .. relativePath]);

        return File.Exists(candidate)
            ? candidate
            : throw new FileNotFoundException(
                $"The file {Path.Combine(relativePath)} was not found in docker/keycloak under the repository root.");
    }

    /// <summary>
    /// Finds a Keycloak file of the tests (the bootstrap file and the test tenant file) in
    /// <c>backend/tests/HealthyFac.Testing/Keycloak</c> under the repository root.
    /// </summary>
    /// <param name="fileName">The file name, for example <c>"healthyfac-test-bootstrap.json"</c>.</param>
    /// <returns>The full path of the file.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the repository root holds no such file.
    /// </exception>
    public static string FindTestFile(string fileName)
    {
        var candidate = Path.Combine([RepositoryRoot.Find().FullName, .. TestFileDirectory, fileName]);

        return File.Exists(candidate)
            ? candidate
            : throw new FileNotFoundException(
                $"The file {fileName} was not found in backend/tests/HealthyFac.Testing/Keycloak under the "
                + "repository root.");
    }
}
