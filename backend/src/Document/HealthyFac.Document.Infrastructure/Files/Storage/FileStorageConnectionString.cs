namespace HealthyFac.Document.Infrastructure.Files.Storage;

/// <summary>
/// Reads the key and value pairs of a storage connection string.
/// </summary>
public static class FileStorageConnectionString
{
    private const char PairSeparator = ';';
    private const char ValueSeparator = '=';

    /// <summary>
    /// Splits the connection string into its settings. A value may hold the value separator (the account key is
    /// Base64), so the split happens at the first separator of each pair.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The settings with case-insensitive keys. Empty values stay out.</returns>
    public static IReadOnlyDictionary<string, string> Parse(string connectionString) =>
        connectionString
            .Split(PairSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split(ValueSeparator, count: 2))
            .Where(parts => parts.Length == 2 && parts[1].Length > 0)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
}
