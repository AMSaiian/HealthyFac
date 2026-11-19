using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;

namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// Judges the forecast time to an alarm limit. It needs ready statistics and an alarm limit in the slope direction.
/// </summary>
public static class TrendRule
{
    /// <summary>
    /// Builds the conditions of one sample. A counter has no noise guard: its slope is never noise.
    /// </summary>
    /// <param name="value">The sample value.</param>
    /// <param name="forecastSeconds">The seconds to the alarm limit, or <see langword="null"/>.</param>
    /// <param name="slope">The slope per second.</param>
    /// <param name="sigma">The sigma of the baseline.</param>
    /// <param name="behavior">The behavior of the metric.</param>
    /// <param name="settings">The rule settings.</param>
    /// <returns>The conditions and the numbers of the sample.</returns>
    public static (RuleCondition Condition, SignalSnapshot Snapshot) Evaluate(
        double value,
        double? forecastSeconds,
        double slope,
        double sigma,
        MetricBehavior behavior,
        RuleSettings settings
    )
    {
        var isNearLimit = forecastSeconds is > 0 && forecastSeconds <= settings.HorizonSeconds;
        var isAboveNoise = behavior == MetricBehavior.Counter
            || Math.Abs(slope) * settings.HorizonSeconds > settings.NoiseGuardFactor * sigma;
        var isTrending = isNearLimit && isAboveNoise;

        return (new RuleCondition { IsOpen = isTrending, IsAlarm = false, IsResolved = !isTrending },
            new SignalSnapshot { Value = value, ForecastSeconds = isTrending ? forecastSeconds : null });
    }
}
