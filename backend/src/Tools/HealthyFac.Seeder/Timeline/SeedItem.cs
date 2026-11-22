namespace HealthyFac.Seeder.Timeline;

/// <summary>Builds and reads the item name of a seed event: the file and the key of the item in it.</summary>
public static class SeedItem
{
    private const char Separator = '#';

    public static string Of(string file, string key) => $"{file}{Separator}{key}";

    /// <summary>Reads the file of an item name.</summary>
    /// <param name="item">The item name.</param>
    /// <returns>The file, or the whole item name when it has no separator.</returns>
    public static string FileOf(string item)
    {
        var index = item.IndexOf(Separator, StringComparison.Ordinal);

        return index < 0 ? item : item[..index];
    }
}
