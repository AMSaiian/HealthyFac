namespace HealthyFac.Telemetry.Domain.SignalEpisodes;

/// <summary>
/// The numbers that explain a change of an episode: the value that the rule saw, the limit, the z value, and the
/// forecast. The phase code names the phase of the sample. A number that does not apply to the rule is
/// <see langword="null"/>.
/// </summary>
public sealed record SignalSnapshot
{
    public static SignalSnapshot Empty { get; } = new();

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public double? Z { get; init; }

    public double? ForecastSeconds { get; init; }

    public string? PhaseCode { get; init; }
}
