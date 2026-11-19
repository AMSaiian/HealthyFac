using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Infrastructure.MetricDefinitions.Persistence;

/// <summary>
/// Defines the column names of the limit values and the default of the behavior column. The simulator reads the
/// table, so the columns stay additive: an insert without them gets no limits and the level behavior.
/// </summary>
public static class MetricDefinitionDatabaseConstants
{
    public static class LimitColumns
    {
        public const string WarningLow = "warning_low";
        public const string WarningHigh = "warning_high";
        public const string AlarmLow = "alarm_low";
        public const string AlarmHigh = "alarm_high";
    }

    public static readonly string DefaultBehaviorSql = $"'{MetricBehavior.Level.Value}'";
}
