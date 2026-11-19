using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.UnitTests.Evaluation.Data;

public static class RuleTestData
{
    public static TheoryData<string, MetricLimits, MetricLimits, MetricLimits> LooserCases => new()
    {
        {
            "high limits take the higher value",
            new MetricLimits { WarningHigh = 80, AlarmHigh = 90 },
            new MetricLimits { WarningHigh = 85, AlarmHigh = 120 },
            new MetricLimits { WarningHigh = 85, AlarmHigh = 120 }
        },
        {
            "low limits take the lower value",
            new MetricLimits { WarningLow = 20, AlarmLow = 10 },
            new MetricLimits { WarningLow = 15, AlarmLow = 5 },
            new MetricLimits { WarningLow = 15, AlarmLow = 5 }
        },
        {
            "a side that is empty in the candidate stays empty",
            new MetricLimits { AlarmLow = 10, AlarmHigh = 90 },
            new MetricLimits(),
            new MetricLimits()
        },
        {
            "a side that is empty in the current phase stays empty",
            new MetricLimits(),
            new MetricLimits { AlarmLow = 10, AlarmHigh = 90 },
            new MetricLimits()
        },
        {
            "an empty side does not empty the other side",
            new MetricLimits { AlarmHigh = 90 },
            new MetricLimits { AlarmLow = 5, AlarmHigh = 100 },
            new MetricLimits { AlarmHigh = 100 }
        },
    };
}
