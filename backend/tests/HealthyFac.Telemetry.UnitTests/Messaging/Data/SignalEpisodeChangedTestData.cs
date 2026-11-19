using HealthyFac.Telemetry.Messaging.Signals;

namespace HealthyFac.Telemetry.UnitTests.Messaging.Data;

public static class SignalEpisodeChangedTestData
{
    private static readonly DateTime OpenedAt = new(
        year: 2026, month: 11, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static TheoryData<string, SignalEpisodeChanged, string> InvalidMessages => new()
    {
        { "sequence 0", Valid() with { Sequence = 0 }, SignalMessageErrors.Validation.SequenceTooLow },
        {
            "resolved at on an open state",
            Valid() with { ResolvedAt = OpenedAt.AddMinutes(value: 1) },
            SignalMessageErrors.Validation.ResolvedAtWithOpenState
        },
        {
            "no resolved at on a resolved state",
            Valid() with { State = SignalEpisodeStateValue.Resolved },
            SignalMessageErrors.Validation.ResolvedAtRequired
        },
    };

    public static TheoryData<string, SignalEpisodeChanged> ValidMessages => new()
    {
        { "open", Valid() },
        {
            "resolved",
            Valid() with
            {
                State = SignalEpisodeStateValue.Resolved, ResolvedAt = OpenedAt.AddMinutes(value: 1), Sequence = 2,
            }
        },
    };

    public static SignalEpisodeChanged Valid() =>
        new()
        {
            EpisodeId = Guid.CreateVersion7(),
            MachineId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            Rule = SignalRuleValue.Threshold,
            Severity = SignalSeverityValue.Warning,
            State = SignalEpisodeStateValue.Open,
            MetricKey = "spindle.temperature",
            OpenedAt = OpenedAt,
            Sequence = 1,
            ChangedAt = OpenedAt,
        };
}
