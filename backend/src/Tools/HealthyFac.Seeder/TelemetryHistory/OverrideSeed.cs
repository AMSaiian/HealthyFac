using System.Text.Json;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>Values and status codes that replace the ones of the samples while the override lasts.</summary>
public sealed class OverrideSeed
{
    public required string Key { get; init; }

    public required string Machine { get; init; }

    /// <summary>The metric key to a JSON value: a number, a boolean, or a state text.</summary>
    public required IReadOnlyDictionary<string, JsonElement> Values { get; init; }

    /// <summary>The status codes that replace the codes of the sample. An empty list means no code.</summary>
    public required IReadOnlyList<string> Codes { get; init; }

    public required RelativeMoment From { get; init; }

    public required RelativeMoment Until { get; init; }
}
