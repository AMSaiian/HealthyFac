using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.UnitTests.Evaluation.Data;

namespace HealthyFac.Telemetry.UnitTests.Evaluation;

public sealed class PhaseMatcherTests
{
    private static readonly DateTime Start = SignalSeries.Start;

    [Theory]
    [MemberData(nameof(PhaseMatcherTestData.OperatorCases), MemberType = typeof(PhaseMatcherTestData))]
    public void Match_WithEachOperator_MatchesOnlyWhenTheConditionHolds(
        string caseName,
        OperatingPhaseCondition condition,
        SampleValues holding,
        SampleValues failing
    )
    {
        var phase = PhaseMatcherTestData.Phase(code: "p", priority: 1, condition);

        var matchedHolding = PhaseMatcher.Match([phase], holding);
        var matchedFailing = PhaseMatcher.Match([phase], failing);

        matchedHolding.Should().Be(phase.Id, caseName);
        matchedFailing.Should().BeNull(caseName);
    }

    [Theory]
    [MemberData(nameof(PhaseMatcherTestData.MissingValueCases), MemberType = typeof(PhaseMatcherTestData))]
    public void Match_WithAMetricThatHasNoValueInTheSample_IsFalse(string caseName, OperatingPhaseCondition condition)
    {
        var phase = PhaseMatcherTestData.Phase(code: "p", priority: 1, condition);

        var matched = PhaseMatcher.Match([phase], new SampleValues());

        matched.Should().BeNull(caseName);
    }

    [Fact]
    public void Match_WithTwoMatchingPhases_TakesTheFirstByPriority()
    {
        var low = PhaseMatcherTestData.Phase(code: "low", priority: 5, PhaseMatcherTestData.NumericAbove(value: 1));
        var high = PhaseMatcherTestData.Phase(code: "high", priority: 1, PhaseMatcherTestData.NumericAbove(value: 1));

        var matched = PhaseMatcher.Match([low, high], PhaseMatcherTestData.WithNumber(value: 50));

        matched.Should().Be(high.Id);
    }

    [Fact]
    public void Match_WithAPhaseWhereOneConditionFails_SkipsIt()
    {
        var phase = PhaseMatcherTestData.Phase(
            code: "both",
            priority: 1,
            PhaseMatcherTestData.NumericAbove(value: 1),
            PhaseMatcherTestData.Condition(
                PhaseConditionSource.Status(PhaseMatcherTestData.StatusId),
                PhaseConditionOperator.Active));

        var matched = PhaseMatcher.Match([phase], PhaseMatcherTestData.WithNumber(value: 50));

        matched.Should().BeNull();
    }

    [Fact]
    public void Match_WithoutPhases_IsNull()
    {
        var matched = PhaseMatcher.Match([], PhaseMatcherTestData.WithNumber(value: 50));

        matched.Should().BeNull();
    }

    [Fact]
    public void Advance_WithAMismatchShorterThanTheDwell_KeepsThePhaseAndClosesTheGate()
    {
        var tracking = TrackingIn(Guid.CreateVersion7());
        var other = Guid.CreateVersion7();
        var current = tracking.CurrentPhaseId;
        var time = Start.AddMinutes(value: 10);

        tracking.Advance(other, time, PhaseMatcherTestData.Settings);

        var step = tracking.Advance(other, time.AddSeconds(value: 14), PhaseMatcherTestData.Settings);

        step.IsLearningOpen.Should().BeFalse();
        step.IsSwitched.Should().BeFalse();
        tracking.CurrentPhaseId.Should().Be(current);
    }

    [Fact]
    public void Advance_WithAMismatchThatReachesTheDwell_SwitchesThePhase()
    {
        var tracking = TrackingIn(Guid.CreateVersion7());
        var other = Guid.CreateVersion7();
        var time = Start.AddMinutes(value: 10);
        tracking.Advance(other, time, PhaseMatcherTestData.Settings);

        var step = tracking.Advance(other, time.AddSeconds(value: 15), PhaseMatcherTestData.Settings);

        step.IsSwitched.Should().BeTrue();
        step.IsLearningOpen.Should().BeFalse();
        tracking.CurrentPhaseId.Should().Be(other);
        tracking.SwitchedAt.Should().Be(time.AddSeconds(value: 15));
    }

    [Fact]
    public void Advance_AfterASwitch_KeepsTheGateClosedForTheSettleTime()
    {
        var tracking = TrackingIn(Guid.CreateVersion7());
        var other = Guid.CreateVersion7();
        var time = Start.AddMinutes(value: 10);
        tracking.Advance(other, time, PhaseMatcherTestData.Settings);
        var switchTime = time.AddSeconds(value: 15);
        tracking.Advance(other, switchTime, PhaseMatcherTestData.Settings);

        var beforeSettle = tracking.Advance(other, switchTime.AddSeconds(value: 29), PhaseMatcherTestData.Settings);
        var afterSettle = tracking.Advance(other, switchTime.AddSeconds(value: 30), PhaseMatcherTestData.Settings);

        beforeSettle.IsLearningOpen.Should().BeFalse();
        afterSettle.IsLearningOpen.Should().BeTrue();
    }

    [Fact]
    public void Advance_WithAStableMatch_KeepsTheGateOpen()
    {
        var phase = Guid.CreateVersion7();
        var tracking = TrackingIn(phase);

        var step = tracking.Advance(phase, Start.AddMinutes(value: 10), PhaseMatcherTestData.Settings);

        step.IsLearningOpen.Should().BeTrue();
    }

    [Fact]
    public void Advance_WhileNothingMatches_GrowsUnmatchedSinceAndClearsItOnAMatch()
    {
        var phase = Guid.CreateVersion7();
        var tracking = TrackingIn(phase);
        var time = Start.AddMinutes(value: 10);

        tracking.Advance(matched: null, time, PhaseMatcherTestData.Settings);
        tracking.Advance(matched: null, time.AddSeconds(value: 5), PhaseMatcherTestData.Settings);
        var whileUnmatched = tracking.UnmatchedSince;
        tracking.Advance(phase, time.AddSeconds(value: 6), PhaseMatcherTestData.Settings);

        whileUnmatched.Should().Be(time);
        tracking.UnmatchedSince.Should().BeNull();
    }

    [Fact]
    public void Advance_WhileTheCurrentPhaseIsNull_KeepsTheGateClosed()
    {
        var tracking = new PhaseTracking();

        var step = tracking.Advance(matched: null, Start, PhaseMatcherTestData.Settings);

        step.IsLearningOpen.Should().BeFalse();
    }

    private static PhaseTracking TrackingIn(Guid phaseId)
    {
        var tracking = new PhaseTracking();
        var time = Start;
        tracking.Advance(phaseId, time, PhaseMatcherTestData.Settings);
        tracking.Advance(phaseId, time.AddSeconds(value: 15), PhaseMatcherTestData.Settings);
        tracking.Advance(phaseId, time.AddSeconds(value: 60), PhaseMatcherTestData.Settings);

        return tracking;
    }
}
