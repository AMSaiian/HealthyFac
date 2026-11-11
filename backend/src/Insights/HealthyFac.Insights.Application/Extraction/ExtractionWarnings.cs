using System.Text.Json;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Builds the warnings that the user sees next to a completed attempt. A warning is a translation key with values, as
/// JSON, so the web app shows it in the language of the user.
/// </summary>
public static class ExtractionWarnings
{
    public const string TruncatedKey = "INGEST.WARNINGS.TRUNCATED";

    public const string MachinesKind = "machines";
    public const string MetricsKind = "metrics";
    public const string StatusesKind = "statuses";

    /// <summary>
    /// Builds the warning for a list that the mapper cut to its cap.
    /// </summary>
    /// <param name="kind">What was cut: machines, metrics, or statuses.</param>
    /// <param name="found">The number of entries the extraction held.</param>
    /// <param name="cap">The number of entries the mapper kept.</param>
    /// <returns>The JSON text with the key and the values.</returns>
    public static string Truncated(string kind, int found, int cap) =>
        JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["key"] = TruncatedKey,
                ["values"] = new Dictionary<string, object> { ["found"] = found, ["cap"] = cap, ["kind"] = kind },
            });
}
