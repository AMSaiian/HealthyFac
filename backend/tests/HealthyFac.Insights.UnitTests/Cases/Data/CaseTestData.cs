using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;

namespace HealthyFac.Insights.UnitTests.Cases.Data;

public static class CaseTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid CaseId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static MaintenanceCase CreateCase() => new(CompanyId, MachineId, Now);

    public static MachineSignal CreateSignalWithValues(double value, double limit) =>
        new(
            Guid.CreateVersion7(),
            MachineId,
            CompanyId,
            new SignalChange
            {
                Rule = SignalRule.Threshold,
                Severity = SignalSeverity.Warning,
                State = SignalState.Open,
                Value = value,
                Limit = limit,
                OpenedAt = Now,
                Sequence = 1,
            });

    public static MachineSignal CreateSignal(
        SignalState? state = null,
        SignalCoverage? coverage = null
    )
    {
        var effectiveState = state ?? SignalState.Open;

        var signal = new MachineSignal(
            Guid.CreateVersion7(),
            MachineId,
            CompanyId,
            new SignalChange
            {
                Rule = SignalRule.Threshold,
                Severity = SignalSeverity.Warning,
                State = effectiveState,
                OpenedAt = Now,
                ResolvedAt = effectiveState == SignalState.Resolved ? Now.AddMinutes(value: 1) : null,
                Sequence = 1,
            });

        if (coverage == SignalCoverage.Watched)
        {
            signal.Watch();
        }
        else if (coverage == SignalCoverage.Dismissed)
        {
            signal.Dismiss();
        }
        else if (coverage == SignalCoverage.Drafted)
        {
            signal.MarkDrafted(Guid.CreateVersion7());
        }
        else if (coverage == SignalCoverage.Linked)
        {
            signal.MarkLinked(Guid.CreateVersion7());
        }

        return signal;
    }

    public static TheoryData<string, SignalCoverage, SignalCoverage> CoverageAfterClose =>
        new()
        {
            { "newIsDismissed", SignalCoverage.New, SignalCoverage.Dismissed },
            { "watchedIsDismissed", SignalCoverage.Watched, SignalCoverage.Dismissed },
            { "draftedIsKept", SignalCoverage.Drafted, SignalCoverage.Drafted },
            { "linkedIsKept", SignalCoverage.Linked, SignalCoverage.Linked },
            { "dismissedIsKept", SignalCoverage.Dismissed, SignalCoverage.Dismissed },
        };
}
