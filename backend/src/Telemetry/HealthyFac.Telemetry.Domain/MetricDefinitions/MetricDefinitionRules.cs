using System.Text.RegularExpressions;

namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// Defines the rules of a metric key and of a state code. Both follow one pattern, so a key and a code have one
/// definition. The pattern is lower case, so the payload keys match without a case rule.
/// </summary>
public static partial class MetricDefinitionRules
{
    public const string KeyPattern = "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)*$";
    public const int KeyMaxLength = 100;
    public const int MinStateCount = 2;

    [GeneratedRegex(KeyPattern, RegexOptions.CultureInvariant)]
    private static partial Regex KeyRegex();

    /// <summary>
    /// Checks the length and the pattern of a key or a state code. It never throws.
    /// </summary>
    /// <param name="value">The text to check, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is a valid key.</returns>
    public static bool IsValidKey(string? value) =>
        value is { Length: > 0 and <= KeyMaxLength } && KeyRegex().IsMatch(value);
}
