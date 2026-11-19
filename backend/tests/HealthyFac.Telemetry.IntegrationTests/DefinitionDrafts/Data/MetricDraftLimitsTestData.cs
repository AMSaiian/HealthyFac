using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

public static class MetricDraftLimitsTestData
{
    public static readonly object StoredLimits = new
    {
        WarningLow = 10.0,
        WarningHigh = 80.0,
        AlarmLow = 5.0,
        AlarmHigh = 90.0,
        Behavior = DraftMessageLimits.Behaviors.Counter,
    };

    public static readonly object NoLimits = new
    {
        WarningLow = (double?)null,
        WarningHigh = (double?)null,
        AlarmLow = (double?)null,
        AlarmHigh = (double?)null,
        Behavior = (string?)null,
    };

    public static ProposedMetric ProposedMetricWithLimits() =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            Key = "motor.temperature",
            ValueKind = DraftMessageLimits.ValueKinds.Numeric,
            WarningLow = 10,
            WarningHigh = 80,
            AlarmLow = 5,
            AlarmHigh = 90,
            Behavior = DraftMessageLimits.Behaviors.Counter,
        };
}
