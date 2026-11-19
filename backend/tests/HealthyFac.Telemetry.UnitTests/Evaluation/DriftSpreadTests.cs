using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.UnitTests.Evaluation.Data;

namespace HealthyFac.Telemetry.UnitTests.Evaluation;

public sealed class DriftSpreadTests
{
    private const int TwoHours = 7200;

    private static readonly StatisticsSettings Settings = SignalSeries.Settings;

    [Fact]
    public void Run_OnTwoHoursOfWhiteNoiseAtOneSecond_OpensNoDriftAndNoTrend()
    {
        var pipeline = new DriftPipeline();

        pipeline.Run(SignalSeries.Noisy(mean: 50, TwoHours, SignalSeries.Start));

        pipeline.DriftDecisions.Should().NotBeEmpty();
        pipeline.DriftDecisions.Should().OnlyContain(entry => entry.Decision.Kind == RuleDecisionKind.None);
        pipeline.TrendDecisions.Should().OnlyContain(entry => entry.Decision.Kind == RuleDecisionKind.None);
    }

    [Fact]
    public void Run_OnTwoHoursOfASixtySecondSineAtOneSecond_OpensNoDriftAndNoTrend()
    {
        var pipeline = new DriftPipeline();

        pipeline.Run(SignalSeries.Sine(mean: 50, amplitude: 5, periodSeconds: 60, TwoHours, SignalSeries.Start));

        pipeline.DriftDecisions.Should().NotBeEmpty();
        pipeline.DriftDecisions.Should().OnlyContain(entry => entry.Decision.Kind == RuleDecisionKind.None);
        pipeline.TrendDecisions.Should().OnlyContain(entry => entry.Decision.Kind == RuleDecisionKind.None);
    }

    [Fact]
    public void Run_WithAStepOfFiveSigma_OpensDriftAfterThePersistenceDuration()
    {
        var pipeline = new DriftPipeline();
        pipeline.Run(SignalSeries.Noisy(mean: 50, count: 3600, SignalSeries.Start));
        pipeline.ForgetFirstConditions();
        var stepStart = SignalSeries.Start.AddSeconds(value: 3600);

        pipeline.Run(SignalSeries.Noisy(mean: 55, count: 600, stepStart, seed: 8));

        var opened = pipeline.DriftDecisions.First(entry => entry.Decision.Kind == RuleDecisionKind.Open);

        pipeline.FirstDriftConditionAt.Should().NotBeNull();
        (opened.Time - pipeline.FirstDriftConditionAt!.Value).TotalSeconds
            .Should()
            .BeGreaterThanOrEqualTo(DriftPipeline.Rules.DriftDurationSeconds);
    }

    [Fact]
    public void Run_WithARamp_OpensTrendAfterThePersistenceDuration()
    {
        var pipeline = new DriftPipeline();
        pipeline.Run(SignalSeries.Noisy(mean: 10, count: 600, SignalSeries.Start));
        pipeline.ForgetFirstConditions();

        pipeline.Run(
            SignalSeries.Ramp(from: 10, perSecond: 0.1, count: 1000, SignalSeries.Start.AddSeconds(value: 600)));

        var opened = pipeline.TrendDecisions.First(entry => entry.Decision.Kind == RuleDecisionKind.Open);

        pipeline.FirstTrendConditionAt.Should().NotBeNull();
        (opened.Time - pipeline.FirstTrendConditionAt!.Value).TotalSeconds
            .Should()
            .BeGreaterThanOrEqualTo(DriftPipeline.Rules.TrendDurationSeconds);
    }

    [Fact]
    public void SigmaFast_OnWhiteNoise_StaysNearTheWhiteNoiseValue()
    {
        var pipeline = new DriftPipeline();
        pipeline.Run(SignalSeries.Noisy(mean: 50, TwoHours, SignalSeries.Start));
        var lambda = 1 - Math.Exp(-1.0 / Settings.FastTauSeconds);
        var whiteNoise = pipeline.Statistics.Sigma(min: 0, max: 200, Settings) * Math.Sqrt(lambda / (2 - lambda));

        var sigmaFast = pipeline.Statistics.SigmaFast(min: 0, max: 200, Settings);

        sigmaFast.Should().BeGreaterThanOrEqualTo(whiteNoise);
        sigmaFast.Should().BeLessThan(whiteNoise * 1.5);
    }

    [Fact]
    public void SigmaFast_OnTheSine_EqualsTheRootOfTheDriftSpread()
    {
        var pipeline = new DriftPipeline();
        pipeline.Run(SignalSeries.Sine(mean: 50, amplitude: 5, periodSeconds: 60, TwoHours, SignalSeries.Start));

        var sigmaFast = pipeline.Statistics.SigmaFast(min: 0, max: 200, Settings);

        sigmaFast.Should().Be(Math.Sqrt(pipeline.Statistics.DriftSpread));
    }

    [Fact]
    public void Apply_WithAFrozenBaseline_KeepsTheDriftSpread()
    {
        var pipeline = new DriftPipeline();
        pipeline.Run(SignalSeries.Noisy(mean: 50, count: 600, SignalSeries.Start));
        var spread = pipeline.Statistics.DriftSpread;
        var count = pipeline.Statistics.DriftSpreadCount;

        pipeline.Run(
            SignalSeries.Noisy(mean: 70, count: 100, SignalSeries.Start.AddSeconds(value: 600)),
            isFrozen: true);

        pipeline.Statistics.DriftSpread.Should().Be(spread);
        pipeline.Statistics.DriftSpreadCount.Should().Be(count);
    }

    [Fact]
    public void IsDriftReady_WaitsForTheWarmUpSamplesAfterTheBaselineWarmUp()
    {
        var series = SignalSeries.Noisy(mean: 50, 2 * Settings.WarmUpSamples, SignalSeries.Start);
        var statistics = new MetricStatistics(Guid.CreateVersion7(), operatingPhaseId: null);

        foreach (var (time, value) in series.Take(2 * Settings.WarmUpSamples - 1))
        {
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Level, Settings);
        }

        var isReadyBefore = statistics.IsDriftReady(Settings);
        statistics.Apply(series[^1].Value, series[^1].Time, isFrozen: false, MetricBehavior.Level, Settings);

        isReadyBefore.Should().BeFalse();
        statistics.IsReady(Settings).Should().BeTrue();
        statistics.IsDriftReady(Settings).Should().BeTrue();
    }

    [Fact]
    public void Observe_WithEnoughSamplesButTooShortAStreak_OpensNothingUntilTheDuration()
    {
        var streak = new RuleStreak(Guid.CreateVersion7(), SignalRule.Drift, subjectId: null);
        var condition = new RuleCondition { IsOpen = true, IsAlarm = false, IsResolved = false };
        var duration = TimeSpan.FromSeconds(seconds: 40);

        var first = Observe(streak, condition, offsetSeconds: 0, duration);
        var second = Observe(streak, condition, offsetSeconds: 1, duration);
        var late = Observe(streak, condition, offsetSeconds: 40, duration);

        first.Kind.Should().Be(RuleDecisionKind.None);
        second.Kind.Should().Be(RuleDecisionKind.None);
        late.Kind.Should().Be(RuleDecisionKind.Open);
    }

    private static RuleDecision Observe(
        RuleStreak streak,
        RuleCondition condition,
        int offsetSeconds,
        TimeSpan duration
    ) =>
        streak.Observe(
            condition,
            openSeverity: null,
            SignalSeries.Start.AddSeconds(offsetSeconds),
            triggerPersistence: 2,
            resolvePersistence: 2,
            SignalSnapshot.Empty,
            duration);
}
