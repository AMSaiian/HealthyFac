namespace HealthyFac.Shared.Infrastructure.Configuration;

/// <summary>
/// Loads the nearest dotenv file into the process environment.
/// This sits in the infrastructure layer because design-time tooling needs it too, and that tooling never
/// builds a web host.
/// </summary>
public static class DotEnvLoader
{
    /// <summary>
    /// Loads the nearest <c>.env</c> file into the process environment, searching the current directory and
    /// its parents. Existing environment variables win over file entries, so a value exported by the host or
    /// a container is never overwritten.
    /// </summary>
    /// <returns>The loaded file path, or <see langword="null"/> when no file was found.</returns>
    public static string? Load()
    {
        var path = FindDotEnvFile(Directory.GetCurrentDirectory());
        if (path is null)
        {
            return null;
        }

        DotNetEnv.Env.Load(
            path,
            new DotNetEnv.LoadOptions(setEnvVars: true, clobberExistingVars: false));

        return path;
    }

    private static string? FindDotEnvFile(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var depth = 0; depth < DotEnvConstants.MaxParentDirectoryDepth && directory is not null; depth++)
        {
            var candidate = Path.Combine(directory.FullName, DotEnvConstants.FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
