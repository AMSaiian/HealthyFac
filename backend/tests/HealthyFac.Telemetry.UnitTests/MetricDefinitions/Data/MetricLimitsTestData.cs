using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.UnitTests.MetricDefinitions.Data;

public static class MetricLimitsTestData
{
    public static TheoryData<string, MetricValueKind, MetricLimits> InvalidLimits => new()
    {
        {
            "warning low below alarm low",
            MetricValueKind.Numeric,
            new MetricLimits { WarningLow = 10, AlarmLow = 20 }
        },
        {
            "warning high above alarm high",
            MetricValueKind.Numeric,
            new MetricLimits { WarningHigh = 90, AlarmHigh = 80 }
        },
        {
            "a limit on a boolean metric",
            MetricValueKind.Boolean,
            new MetricLimits { AlarmHigh = 1 }
        },
        {
            "a limit on a state set metric",
            MetricValueKind.StateSet,
            new MetricLimits { WarningLow = 1 }
        },
    };

    public static TheoryData<string, MetricValueKind, MetricLimits> ValidLimits => new()
    {
        {
            "equal warning and alarm limits",
            MetricValueKind.Numeric,
            new MetricLimits { WarningLow = 10, AlarmLow = 10, WarningHigh = 90, AlarmHigh = 90 }
        },
        {
            "limits on the high side only",
            MetricValueKind.Numeric,
            new MetricLimits { WarningHigh = 80, AlarmHigh = 90 }
        },
        {
            "limits on the low side only",
            MetricValueKind.Numeric,
            new MetricLimits { WarningLow = 20, AlarmLow = 10 }
        },
        {
            "no limit on a boolean metric",
            MetricValueKind.Boolean,
            MetricLimits.None
        },
    };
}
