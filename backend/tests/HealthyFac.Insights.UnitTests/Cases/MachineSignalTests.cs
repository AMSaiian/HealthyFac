using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.UnitTests.Cases.Data;

namespace HealthyFac.Insights.UnitTests.Cases;

public sealed class MachineSignalTests
{
    private const double CrossingValue = 91;
    private const double CrossingLimit = 90;

    [Fact]
    public void Apply_WithAResolveThatHasNoLimit_KeepsTheValueAndTheLimitOfTheCrossing()
    {
        var signal = CaseTestData.CreateSignalWithValues(CrossingValue, CrossingLimit);

        var resolve = new SignalChange
        {
            Rule = SignalRule.Threshold,
            Severity = SignalSeverity.Warning,
            State = SignalState.Resolved,
            Value = 0,
            Limit = null,
            OpenedAt = CaseTestData.Now,
            ResolvedAt = CaseTestData.Now.AddMinutes(value: 5),
            Sequence = 2,
        };

        var isApplied = signal.Apply(resolve);

        isApplied.Should().BeTrue();
        signal.Value.Should().Be(CrossingValue);
        signal.Limit.Should().Be(CrossingLimit);
        signal.ResolvedAt.Should().Be(CaseTestData.Now.AddMinutes(value: 5));
    }

    [Fact]
    public void Apply_WithAnEscalationOfAnOpenSignal_TakesTheNewValueAndLimit()
    {
        var signal = CaseTestData.CreateSignalWithValues(CrossingValue, CrossingLimit);

        var escalation = new SignalChange
        {
            Rule = SignalRule.Threshold,
            Severity = SignalSeverity.Alarm,
            State = SignalState.Open,
            Value = 120,
            Limit = 100,
            OpenedAt = CaseTestData.Now,
            Sequence = 2,
        };

        signal.Apply(escalation);

        signal.Value.Should().Be(expected: 120);
        signal.Limit.Should().Be(expected: 100);
    }
}
