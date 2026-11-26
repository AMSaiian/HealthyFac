namespace HealthyFac.Testing.Repository;

public static class RepositoryRoot
{
    private const string GitEntryName = ".git";

    /// <summary>
    /// Walks up from the test output folder to the folder that holds the <c>.git</c> entry (a folder, or a file in a
    /// git worktree).
    /// </summary>
    /// <returns>The repository root.</returns>
    /// <exception cref="DirectoryNotFoundException">No folder above the output holds a <c>.git</c> entry.</exception>
    public static DirectoryInfo Find()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (!HasGitEntry(folder))
        {
            folder = folder.Parent ?? throw new DirectoryNotFoundException("No folder with .git above the output.");
        }

        return folder;
    }

    private static bool HasGitEntry(DirectoryInfo folder) =>
        Directory.Exists(Path.Combine(folder.FullName, GitEntryName))
        || File.Exists(Path.Combine(folder.FullName, GitEntryName));
}
