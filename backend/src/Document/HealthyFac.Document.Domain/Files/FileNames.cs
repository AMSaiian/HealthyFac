namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// Holds the rules for the stored name of a file. The service stores the name without the extension. The upload and
/// the rename call these functions, so both apply one definition.
/// </summary>
public static class FileNames
{
    private const char ExtensionSeparator = '.';

    private static readonly char[] PathSeparators = ['/', '\\'];

    /// <summary>
    /// Takes the part after the last path separator, then removes the text from the last dot to the end.
    /// </summary>
    /// <param name="clientFileName">The file name as the client sent it. It may hold a path.</param>
    /// <returns>The base name, or <see langword="null"/> when nothing is left.</returns>
    public static string? ToBaseName(string clientFileName)
    {
        var name = clientFileName[(clientFileName.LastIndexOfAny(PathSeparators) + 1)..];
        var dotIndex = name.LastIndexOf(ExtensionSeparator);

        if (dotIndex >= 0)
        {
            name = name[..dotIndex];
        }

        return name.Length == 0 ? null : name;
    }

    /// <summary>
    /// Takes the text after the last dot of the part after the last path separator, in lower case.
    /// </summary>
    /// <param name="clientFileName">The file name as the client sent it. It may hold a path.</param>
    /// <returns>The extension without the dot, or <see langword="null"/> when the name has no dot or nothing follows
    /// it.</returns>
    public static string? ToExtension(string clientFileName)
    {
        var name = clientFileName[(clientFileName.LastIndexOfAny(PathSeparators) + 1)..];
        var dotIndex = name.LastIndexOf(ExtensionSeparator);

        return dotIndex < 0 || dotIndex == name.Length - 1 ? null : name[(dotIndex + 1)..].ToLowerInvariant();
    }

    /// <summary>
    /// Removes a trailing dot and the stored extension, ignoring case. The service keeps the extension of the stored
    /// type, so a user cannot change it.
    /// </summary>
    /// <param name="name">The name that a user typed.</param>
    /// <param name="extension">The stored extension without the dot.</param>
    /// <returns>The name without the stored extension, or the unchanged name when it does not end with it.</returns>
    public static string StripStoredExtension(string name, string extension)
    {
        var suffix = ExtensionSeparator + extension;

        return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? name[..^suffix.Length] : name;
    }
}
