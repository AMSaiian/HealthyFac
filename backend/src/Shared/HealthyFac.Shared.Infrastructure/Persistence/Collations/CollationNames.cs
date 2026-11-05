namespace HealthyFac.Shared.Infrastructure.Persistence.Collations;

/// <summary>
/// Defines the collation names of the text columns.
/// </summary>
public static class CollationNames
{
    /// <summary>The ICU collation that ignores case but not accents. Text columns use it by default.</summary>
    public const string CaseInsensitive = "case_insensitive";

    /// <summary>The database default collation. A column that opts out of the case-insensitive rule uses it.</summary>
    public const string CaseSensitive = "default";

    public const string CaseInsensitiveLocale = "und-u-ks-level2";
    public const string IcuProvider = "icu";
}
