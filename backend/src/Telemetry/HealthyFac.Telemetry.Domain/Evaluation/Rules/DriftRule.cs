using HealthyFac.Telemetry.Domain.SignalEpisodes;

namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// Judges the z value of the fast mean against the baseline. It needs ready statistics of a tracked phase.
/// </summary>
public static class DriftRule
{
    /// <summary>
    /// Builds the conditions of one sample.
    /// </summary>
    /// <param name="value">The sample value.</param>
    /// <param name="z">The z value.</param>
    /// <param name="settings">The rule settings.</param>
    /// <returns>The conditions and the numbers of the sample.</returns>
    public static (RuleCondition Condition, SignalSnapshot Snapshot) Evaluate(
        double value,
        double? z,
        RuleSettings settings
    )
    {
        var absolute = z is { } zValue ? Math.Abs(zValue) : (double?)null;

        return (new RuleCondition
            {
                IsOpen = absolute > settings.DriftLimit,
                IsAlarm = false,
                IsResolved = absolute < settings.DriftResolveLimit,
            },
            new SignalSnapshot { Value = value, Z = z });
    }
}
