using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.UnitTests.Evaluation.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.Evaluation;

[Feature(TestFeatures.Evaluation)]
public sealed class RuleTests
{
    private const int Persistence = 2;

    private static readonly RuleSettings Settings = new()
    {
        Persistence = Persistence,
        DriftLimit = 3,
        DriftResolveLimit = 2,
        HorizonSeconds = 900,
        NoiseGuardFactor = 3,
        DriftDurationSeconds = 40,
        TrendDurationSeconds = 120,
    };

    private static readonly MetricLimits Limits = new()
    {
        WarningLow = 10,
        WarningHigh = 80,
        AlarmLow = 5,
        AlarmHigh = 90,
    };

    [Fact]
    public void Threshold_HighSide_OpensEscalatesAndResolvesAtTheRightSample()
    {
        var streak = NewStreak(SignalRule.Threshold);
        SignalSeverity? open = null;

        var decisions = new[] { 85.0, 85.0, 95.0, 95.0, 50.0, 50.0 }
            .Select(value => Observe(streak, ThresholdRule.Evaluate(value, Limits).Condition, ref open))
            .ToList();

        decisions.Select(decision => decision.Kind)
            .Should()
            .Equal(
                RuleDecisionKind.None,
                RuleDecisionKind.Open,
                RuleDecisionKind.None,
                RuleDecisionKind.Escalate,
                RuleDecisionKind.None,
                RuleDecisionKind.Resolve);

        decisions[1].Severity.Should().Be(SignalSeverity.Warning);
        decisions[3].Severity.Should().Be(SignalSeverity.Alarm);
    }

    [Fact]
    public void Threshold_LowSide_OpensAndResolves()
    {
        var streak = NewStreak(SignalRule.Threshold);
        SignalSeverity? open = null;

        var decisions = new[] { 8.0, 8.0, 50.0, 50.0 }
            .Select(value => Observe(streak, ThresholdRule.Evaluate(value, Limits).Condition, ref open))
            .ToList();

        decisions.Select(decision => decision.Kind)
            .Should()
            .Equal(
                RuleDecisionKind.None,
                RuleDecisionKind.Open,
                RuleDecisionKind.None,
                RuleDecisionKind.Resolve);
    }

    [Fact]
    public void Threshold_AlarmLimitOnly_OpensAlarmAndResolvesInsideTheAlarmLimit()
    {
        var streak = NewStreak(SignalRule.Threshold);
        var limits = new MetricLimits { AlarmHigh = 90 };
        SignalSeverity? open = null;

        var decisions = new[] { 95.0, 95.0, 85.0, 85.0 }
            .Select(value => Observe(streak, ThresholdRule.Evaluate(value, limits).Condition, ref open))
            .ToList();

        decisions[1].Kind.Should().Be(RuleDecisionKind.Open);
        decisions[1].Severity.Should().Be(SignalSeverity.Alarm);
        decisions[3].Kind.Should().Be(RuleDecisionKind.Resolve);
    }

    [Fact]
    public void Threshold_SingleOutlier_OpensNothing()
    {
        var streak = NewStreak(SignalRule.Threshold);
        SignalSeverity? open = null;

        var decisions = new[] { 85.0, 50.0, 85.0, 50.0 }
            .Select(value => Observe(streak, ThresholdRule.Evaluate(value, Limits).Condition, ref open))
            .ToList();

        decisions.Should().OnlyContain(decision => decision.Kind == RuleDecisionKind.None);
    }

    [Fact]
    public void Threshold_SnapshotHoldsTheValueAndTheBreachedLimit()
    {
        var (_, snapshot) = ThresholdRule.Evaluate(value: 95, Limits);

        snapshot.Value.Should().Be(expected: 95);
        snapshot.Limit.Should().Be(expected: 90);
    }

    [Fact]
    public void Drift_ZAboveTheLimit_OpensAndResolvesBelowTheResolveLimit()
    {
        var streak = NewStreak(SignalRule.Drift);
        SignalSeverity? open = null;

        var decisions = new double?[] { 3.5, -3.5, 2.5, 1.5, 1.5 }
            .Select(z => Observe(streak, DriftRule.Evaluate(value: 1, z, Settings).Condition, ref open))
            .ToList();

        decisions.Select(decision => decision.Kind)
            .Should()
            .Equal(
                RuleDecisionKind.None,
                RuleDecisionKind.Open,
                RuleDecisionKind.None,
                RuleDecisionKind.None,
                RuleDecisionKind.Resolve);
    }

    [Fact]
    public void Trend_ARamp_OpensAWarning()
    {
        var statistics =
            SignalSeries.Learn(SignalSeries.Ramp(from: 10, perSecond: 0.1, count: 1500, SignalSeries.Start));

        var sigma = SignalSeries.NoiseSigma;

        var (condition, snapshot) = TrendRule.Evaluate(
            value: 160,
            statistics.ForecastSeconds(alarmLow: null, alarmHigh: 200),
            statistics.Slope,
            sigma,
            MetricBehavior.Level,
            Settings);

        condition.IsOpen.Should().BeTrue();
        snapshot.ForecastSeconds.Should().BeGreaterThan(expected: 0).And.BeLessThanOrEqualTo(expected: 900);
    }

    [Fact]
    public void Trend_ARampWithinTheNoiseGuard_OpensNothing()
    {
        var (condition, _) = TrendRule.Evaluate(
            value: 160,
            forecastSeconds: 600,
            slope: 0.0001,
            sigma: 5,
            MetricBehavior.Level,
            Settings);

        condition.IsOpen.Should().BeFalse();
        condition.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Trend_WithoutAnAlarmLimitInTheSlopeDirection_OpensNothing()
    {
        var (condition, _) = TrendRule.Evaluate(
            value: 160,
            forecastSeconds: null,
            slope: 0.5,
            sigma: 1,
            MetricBehavior.Level,
            Settings);

        condition.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Trend_ACounterNearItsHighAlarmLimit_OpensWithoutTheNoiseGuard()
    {
        var (condition, _) = TrendRule.Evaluate(
            value: 900,
            forecastSeconds: 600,
            slope: 0.0001,
            sigma: 1000,
            MetricBehavior.Counter,
            Settings);

        condition.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Trend_ForecastAlreadyBeyondTheLimit_OpensNothing()
    {
        var (condition, _) = TrendRule.Evaluate(
            value: 210,
            forecastSeconds: -10,
            slope: 1,
            sigma: 1,
            MetricBehavior.Level,
            Settings);

        condition.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Drift_ZBetweenTheTwoLimits_NeitherOpensNorResolves()
    {
        var (condition, _) = DriftRule.Evaluate(value: 1, z: 2.5, Settings);

        condition.IsOpen.Should().BeFalse();
        condition.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Trend_ForecastBeyondTheHorizon_OpensNothing()
    {
        var (condition, _) = TrendRule.Evaluate(
            value: 10,
            forecastSeconds: 901,
            slope: 1,
            sigma: 1,
            MetricBehavior.Level,
            Settings);

        condition.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Status_WarningOpensAWarningAndErrorAnAlarmAtOnce()
    {
        var warning = NewStreak(SignalRule.Status);
        var error = NewStreak(SignalRule.Status);
        SignalSeverity? openWarning = null;
        SignalSeverity? openError = null;

        var warningDecision = warning.Observe(
            StatusRule.Evaluate(StatusSeverity.Warning, isActive: true),
            openWarning,
            SignalSeries.Start,
            triggerPersistence: 1,
            Persistence,
            SignalSnapshot.Empty);

        var errorDecision = error.Observe(
            StatusRule.Evaluate(StatusSeverity.Error, isActive: true),
            openError,
            SignalSeries.Start,
            triggerPersistence: 1,
            Persistence,
            SignalSnapshot.Empty);

        warningDecision.Severity.Should().Be(SignalSeverity.Warning);
        errorDecision.Severity.Should().Be(SignalSeverity.Alarm);
    }

    [Fact]
    public void Status_InfoOpensNothing()
    {
        var condition = StatusRule.Evaluate(StatusSeverity.Info, isActive: true);

        condition.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Status_ResolvesAfterPersistenceSamplesWithoutIt()
    {
        var streak = NewStreak(SignalRule.Status);
        var open = SignalSeverity.Warning;

        var first = streak.Observe(
            StatusRule.Evaluate(StatusSeverity.Warning, isActive: false),
            open,
            SignalSeries.Start,
            triggerPersistence: 1,
            Persistence,
            SignalSnapshot.Empty);

        var second = streak.Observe(
            StatusRule.Evaluate(StatusSeverity.Warning, isActive: false),
            open,
            SignalSeries.Start.AddSeconds(value: 1),
            triggerPersistence: 1,
            Persistence,
            SignalSnapshot.Empty);

        first.Kind.Should().Be(RuleDecisionKind.None);
        second.Kind.Should().Be(RuleDecisionKind.Resolve);
    }

    [Theory]
    [MemberData(nameof(RuleTestData.LooserCases), MemberType = typeof(RuleTestData))]
    public void Looser_WithTwoLimitSets_TakesTheLooserValueOfEachSide(
        string caseName,
        MetricLimits current,
        MetricLimits candidate,
        MetricLimits expected
    )
    {
        var looser = ThresholdRule.Looser(current, candidate);

        looser.Should().Be(expected, caseName);
    }

    [Fact]
    public void Evaluate_WithOpenLimits_OpensWithThemAndResolvesWithTheCurrentLimits()
    {
        var current = new MetricLimits { AlarmHigh = 90 };
        var looser = new MetricLimits { AlarmHigh = 120 };

        var inLooserOnly = ThresholdRule.Evaluate(value: 95, current, looser).Condition;
        var beyondLooser = ThresholdRule.Evaluate(value: 125, current, looser).Condition;

        inLooserOnly.IsAlarm.Should().BeFalse();
        inLooserOnly.IsResolved.Should().BeFalse();
        beyondLooser.IsAlarm.Should().BeTrue();
    }

    [Fact]
    public void Observe_AfterCountFrom_RestartsTheCount()
    {
        var streak = NewStreak(SignalRule.Threshold);
        var breach = ThresholdRule.Evaluate(value: 95, Limits).Condition;
        var settleEnd = SignalSeries.Start.AddSeconds(value: 30);

        streak.Observe(
            breach, openSeverity: null, SignalSeries.Start, Persistence, Persistence, SignalSnapshot.Empty);

        streak.CountFrom(settleEnd);

        var first = streak.Observe(
            breach, openSeverity: null, settleEnd, Persistence, Persistence, SignalSnapshot.Empty);

        var second = streak.Observe(
            breach, openSeverity: null, settleEnd.AddSeconds(value: 1), Persistence, Persistence, SignalSnapshot.Empty);

        first.Kind.Should().Be(RuleDecisionKind.None);
        second.Kind.Should().Be(RuleDecisionKind.Open);
    }

    [Fact]
    public void Observe_BeforeCountsFrom_CountsNothing()
    {
        var streak = NewStreak(SignalRule.Threshold);
        streak.CountFrom(SignalSeries.Start.AddSeconds(value: 10));
        var condition = ThresholdRule.Evaluate(value: 95, Limits).Condition;

        var early = new[] { 0, 1, 2, 3 }.Select(offset => streak.Observe(
                condition,
                openSeverity: null,
                SignalSeries.Start.AddSeconds(offset),
                Persistence,
                Persistence,
                SignalSnapshot.Empty))
            .ToList();

        early.Should().OnlyContain(decision => decision.Kind == RuleDecisionKind.None);
        streak.OpenCount.Should().Be(expected: 0);
    }

    private static RuleStreak NewStreak(SignalRule rule) => new(Guid.CreateVersion7(), rule, Guid.CreateVersion7());

    private static RuleDecision Observe(RuleStreak streak, RuleCondition condition, ref SignalSeverity? open)
    {
        var decision = streak.Observe(
            condition,
            open,
            SignalSeries.Start,
            Persistence,
            Persistence,
            SignalSnapshot.Empty);

        open = decision.Kind switch
        {
            RuleDecisionKind.Open or RuleDecisionKind.Escalate => decision.Severity,
            RuleDecisionKind.Resolve => null,
            _ => open,
        };

        return decision;
    }
}
