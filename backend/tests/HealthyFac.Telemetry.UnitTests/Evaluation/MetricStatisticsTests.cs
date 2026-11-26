using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.UnitTests.Evaluation.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.Evaluation;

[Feature(TestFeatures.Evaluation)]
public sealed class MetricStatisticsTests
{
    private const double Tolerance = 1e-9;

    private static readonly StatisticsSettings Settings = SignalSeries.Settings;

    [Fact]
    public void Apply_WithWarmUpSamples_GivesTheSampleMeanAndVariance()
    {
        var series = SignalSeries.Noisy(mean: 50, Settings.WarmUpSamples, SignalSeries.Start);
        var values = series.Select(sample => sample.Value).ToList();
        var mean = values.Average();
        var variance = values.Sum(value => (value - mean) * (value - mean)) / (values.Count - 1);

        var statistics = SignalSeries.Learn(series);

        statistics.IsWarm.Should().BeTrue();
        statistics.SlowMean.Should().BeApproximately(mean, Tolerance);
        statistics.Variance.Should().BeApproximately(variance, Tolerance);
    }

    [Fact]
    public void Apply_BeforeTheWarmUpEnds_IsNotWarm()
    {
        var series = SignalSeries.Noisy(mean: 50, Settings.WarmUpSamples - 1, SignalSeries.Start);

        var statistics = SignalSeries.Learn(series);

        statistics.IsWarm.Should().BeFalse();
        statistics.IsReady(Settings).Should().BeFalse();
    }

    [Fact]
    public void Apply_WithAStepOfFiveSigma_GivesAZAboveTheDriftLimitWithinFiveSamples()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var stepStart = SignalSeries.Start.AddSeconds(value: 120);
        var zBefore = statistics.Z(min: null, max: null, Settings);

        foreach (var (time, value) in SignalSeries.Constant(value: 55, count: 5, stepStart))
        {
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Level, Settings);
        }

        var zAfter = statistics.Z(min: null, max: null, Settings);

        Math.Abs(zBefore!.Value).Should().BeLessThan(expected: 3);
        zAfter.Should().BeGreaterThan(expected: 3);
    }

    [Fact]
    public void Apply_WithFrozenBaseline_KeepsTheBaselineAndMovesTheTracker()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var slowMean = statistics.SlowMean;
        var variance = statistics.Variance;
        var fastMean = statistics.FastMean;

        foreach (var (time, value) in SignalSeries.Constant(
            value: 60, count: 30, SignalSeries.Start.AddSeconds(value: 120)))
        {
            statistics.Apply(value, time, isFrozen: true, MetricBehavior.Level, Settings);
        }

        statistics.SlowMean.Should().Be(slowMean);
        statistics.Variance.Should().Be(variance);
        statistics.FastMean.Should().BeGreaterThan(fastMean + 5);
    }

    [Fact]
    public void Apply_WithUnfrozenBaseline_MovesTheBaseline()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var slowMean = statistics.SlowMean;

        foreach (var (time, value) in SignalSeries.Constant(
            value: 60, count: 30, SignalSeries.Start.AddSeconds(value: 120)))
        {
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Level, Settings);
        }

        statistics.SlowMean.Should().BeGreaterThan(slowMean);
    }

    [Fact]
    public void Sigma_OnAFlatSignalWithARange_HoldsTheFloor()
    {
        var statistics =
            SignalSeries.Learn(SignalSeries.Constant(value: 50, Settings.WarmUpSamples, SignalSeries.Start));

        var sigma = statistics.Sigma(min: 0, max: 100, Settings);

        sigma.Should().BeApproximately(Settings.SigmaFloorShare * 100, Tolerance);
    }

    [Fact]
    public void Sigma_OnAFlatSignalWithoutARange_HasNoFloor()
    {
        var statistics =
            SignalSeries.Learn(SignalSeries.Constant(value: 50, Settings.WarmUpSamples, SignalSeries.Start));

        var sigmaWithoutRange = statistics.Sigma(min: null, max: null, Settings);
        var sigmaWithMinOnly = statistics.Sigma(min: 0, max: null, Settings);

        sigmaWithoutRange.Should().Be(expected: 0);
        sigmaWithMinOnly.Should().Be(expected: 0);
    }

    [Fact]
    public void Apply_AfterAGap_RestartsTheTrackerAndKeepsTheBaseline()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var slowMean = statistics.SlowMean;
        var variance = statistics.Variance;
        var afterGap = SignalSeries.Start.AddSeconds(120 + Settings.GapSeconds + 1);

        var update = statistics.Apply(x: 80, afterGap, isFrozen: false, MetricBehavior.Level, Settings);

        update.IsTrackerRestarted.Should().BeTrue();
        statistics.FastMean.Should().Be(expected: 80);
        statistics.Level.Should().Be(expected: 80);
        statistics.Slope.Should().Be(expected: 0);
        statistics.SlowMean.Should().Be(slowMean);
        statistics.Variance.Should().Be(variance);
    }

    [Fact]
    public void IsReady_AfterAGap_WaitsForTheWarmUpSamplesAgain()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var restart = SignalSeries.Start.AddSeconds(120 + Settings.GapSeconds + 1);
        var series = SignalSeries.Noisy(mean: 50, Settings.WarmUpSamples, restart);
        var isReadyAfterFiftyNine = false;

        foreach (var (time, value) in series.Take(Settings.WarmUpSamples - 1))
        {
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Level, Settings);
        }

        isReadyAfterFiftyNine = statistics.IsReady(Settings);
        statistics.Apply(series[^1].Value, series[^1].Time, isFrozen: false, MetricBehavior.Level, Settings);

        isReadyAfterFiftyNine.Should().BeFalse();
        statistics.IsReady(Settings).Should().BeTrue();
    }

    [Fact]
    public void Apply_WithUnevenIntervals_EqualsTheClosedFormula()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var slowMean = statistics.SlowMean;
        var variance = statistics.Variance;
        var last = SignalSeries.Start.AddSeconds(value: 119);
        var samples = new[] { (Seconds: 7.0, Value: 52.0), (Seconds: 13.0, Value: 48.0) };
        var time = last;

        foreach (var (seconds, value) in samples)
        {
            var alpha = 1 - Math.Exp(-seconds / Settings.SlowTauSeconds);
            variance = (1 - alpha) * (variance + alpha * (value - slowMean) * (value - slowMean));
            slowMean += alpha * (value - slowMean);
            time = time.AddSeconds(seconds);
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Level, Settings);
        }

        statistics.SlowMean.Should().BeApproximately(slowMean, Tolerance);
        statistics.Variance.Should().BeApproximately(variance, Tolerance);
    }

    [Fact]
    public void ForecastSeconds_OnARamp_EqualsTheDistanceOverTheSlope()
    {
        var perSecond = 0.1;
        var statistics = SignalSeries.Learn(SignalSeries.Ramp(from: 10, perSecond, count: 1500, SignalSeries.Start));
        var lastValue = 10 + perSecond * 1499;
        var expected = (200 - lastValue) / perSecond;

        var forecast = statistics.ForecastSeconds(alarmLow: null, alarmHigh: 200);

        statistics.Slope.Should().BeApproximately(perSecond, perSecond * 0.01);
        forecast.Should().BeApproximately(expected, expected * 0.02);
    }

    [Fact]
    public void ForecastSeconds_WithoutALimitInTheSlopeDirection_IsNull()
    {
        var statistics =
            SignalSeries.Learn(SignalSeries.Ramp(from: 10, perSecond: 0.1, count: 300, SignalSeries.Start));

        var forecast = statistics.ForecastSeconds(alarmLow: 0, alarmHigh: null);

        forecast.Should().BeNull();
    }

    [Fact]
    public void Apply_WithTheSameTimeTwice_IgnoresTheSecondSample()
    {
        var statistics = SignalSeries.Learn(SignalSeries.Noisy(mean: 50, count: 120, SignalSeries.Start));
        var fastMean = statistics.FastMean;
        var lastTime = statistics.LastSampledAt!.Value;

        var update = statistics.Apply(x: 90, lastTime, isFrozen: false, MetricBehavior.Level, Settings);

        update.IsIgnored.Should().BeTrue();
        statistics.FastMean.Should().Be(fastMean);
    }

    [Fact]
    public void Apply_WithACounterThatDropsBack_RestartsTheTracker()
    {
        var statistics = new MetricStatistics(Guid.CreateVersion7(), operatingPhaseId: null);

        foreach (var (time, value) in SignalSeries.Ramp(from: 1000, perSecond: 1, count: 100, SignalSeries.Start))
        {
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Counter, Settings);
        }

        var update = statistics.Apply(
            x: 5,
            SignalSeries.Start.AddSeconds(value: 100),
            isFrozen: false,
            MetricBehavior.Counter,
            Settings);

        update.IsTrackerRestarted.Should().BeTrue();
        statistics.Level.Should().Be(expected: 5);
    }
}
