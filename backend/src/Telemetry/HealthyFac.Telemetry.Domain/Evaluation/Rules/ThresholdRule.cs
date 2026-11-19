using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;

namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// Judges a value against the effective limits of a metric. It runs in every phase, also before warm-up.
/// </summary>
public static class ThresholdRule
{
    /// <summary>
    /// Builds the conditions of one sample. A side with only an alarm limit opens an alarm and resolves inside the
    /// alarm limit.
    /// </summary>
    /// <param name="value">The sample value.</param>
    /// <param name="limits">The effective limits. They decide when an episode resolves.</param>
    /// <param name="openLimits">The limits that decide when an episode opens or escalates, or
    /// <see langword="null"/> for <paramref name="limits"/>. During a phase mismatch these are the looser limits of
    /// the current and the matched phase.</param>
    /// <returns>The conditions and the numbers of the sample.</returns>
    public static (RuleCondition Condition, SignalSnapshot Snapshot) Evaluate(
        double value,
        MetricLimits limits,
        MetricLimits? openLimits = null
    )
    {
        var opening = openLimits ?? limits;
        var isWarningBeyond = value > opening.WarningHigh || value < opening.WarningLow;
        var isAlarmBeyond = value > opening.AlarmHigh || value < opening.AlarmLow;
        var isResolveBeyond = value > (limits.WarningHigh ?? limits.AlarmHigh)
            || value < (limits.WarningLow ?? limits.AlarmLow);

        var limit = isAlarmBeyond ? BreachedLimit(value, opening.AlarmLow, opening.AlarmHigh)
            : isWarningBeyond ? BreachedLimit(value, opening.WarningLow, opening.WarningHigh)
            : null;

        return (new RuleCondition { IsOpen = isWarningBeyond, IsAlarm = isAlarmBeyond, IsResolved = !isResolveBeyond },
            new SignalSnapshot { Value = value, Limit = limit });
    }

    /// <summary>
    /// Builds the limits of a phase mismatch: for each limit the looser value of the two phases. A low limit takes the
    /// lower value and a high limit the higher value. A side that is empty in either phase stays empty, because one
    /// of the phases allows any value there.
    /// </summary>
    /// <param name="current">The limits of the current phase.</param>
    /// <param name="candidate">The limits of the matched phase that waits for the dwell time.</param>
    /// <returns>The looser limits.</returns>
    public static MetricLimits Looser(MetricLimits current, MetricLimits candidate) =>
        new()
        {
            WarningLow = Both(current.WarningLow, candidate.WarningLow, Math.Min),
            WarningHigh = Both(current.WarningHigh, candidate.WarningHigh, Math.Max),
            AlarmLow = Both(current.AlarmLow, candidate.AlarmLow, Math.Min),
            AlarmHigh = Both(current.AlarmHigh, candidate.AlarmHigh, Math.Max),
        };

    private static double? Both(double? first, double? second, Func<double, double, double> pick) =>
        first is { } a && second is { } b ? pick(a, b) : null;

    private static double? BreachedLimit(double value, double? low, double? high) =>
        value > high ? high : low;
}
