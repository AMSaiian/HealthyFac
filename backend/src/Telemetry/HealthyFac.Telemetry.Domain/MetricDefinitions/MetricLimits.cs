namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// Holds the warning and alarm limits of a numeric metric, in the unit of the metric. An empty value means no limit
/// on that side. This type is the one definition of the limit rules: a metric definition and a phase limit row call
/// <see cref="EnsureValid"/>.
/// </summary>
public sealed record MetricLimits
{
    public static MetricLimits None { get; } = new();

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    /// <summary>
    /// Tells whether at least one limit is set.
    /// </summary>
    public bool HasAny => WarningLow is not null
        || WarningHigh is not null
        || AlarmLow is not null
        || AlarmHigh is not null;

    /// <summary>
    /// Checks the limits against the value kind of the metric. Equal warning and alarm limits are valid.
    /// </summary>
    /// <param name="kind">The value kind of the metric.</param>
    /// <exception cref="InvalidOperationException">A limit is set on a metric that is not numeric, the warning low
    /// limit is below the alarm low limit, or the warning high limit is above the alarm high limit.</exception>
    public void EnsureValid(MetricValueKind kind)
    {
        if (HasAny && kind != MetricValueKind.Numeric)
        {
            throw MetricDefinitionInvariantErrors.LimitsOnlyForNumeric(kind);
        }

        if (WarningLow is { } warningLow && AlarmLow is { } alarmLow && warningLow < alarmLow)
        {
            throw MetricDefinitionInvariantErrors.WarningLowBelowAlarmLow(warningLow, alarmLow);
        }

        if (WarningHigh is { } warningHigh && AlarmHigh is { } alarmHigh && warningHigh > alarmHigh)
        {
            throw MetricDefinitionInvariantErrors.WarningHighAboveAlarmHigh(warningHigh, alarmHigh);
        }
    }
}
