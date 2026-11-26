using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.Evaluation.Statistics;

/// <summary>
/// The learned statistics of one numeric metric in one operating phase (<see cref="OperatingPhaseId"/> is
/// <see langword="null"/> for the phase none). It holds a baseline (the warm-up values, the slow mean, the variance,
/// the mean sample interval) and a tracker (the fast mean and the Holt level and slope). The baseline learns from
/// the samples; an open episode of the metric freezes it, and the tracker keeps moving.
/// </summary>
public class MetricStatistics : IEntity
{
    private const double Two = 2;

    /// <summary>
    /// Creates empty statistics.
    /// </summary>
    /// <param name="metricDefinitionId">The metric.</param>
    /// <param name="operatingPhaseId">The phase, or <see langword="null"/> for the phase none.</param>
    public MetricStatistics(Guid metricDefinitionId, Guid? operatingPhaseId)
    {
        MetricDefinitionId = metricDefinitionId;
        OperatingPhaseId = operatingPhaseId;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid MetricDefinitionId { get; private set; }

    public Guid? OperatingPhaseId { get; private set; }

    public int WarmCount { get; private set; }

    public double WarmMean { get; private set; }

    public double WarmM2 { get; private set; }

    public bool IsWarm { get; private set; }

    public double SlowMean { get; private set; }

    public double Variance { get; private set; }

    /// <summary>The mean time between two samples, in seconds. It starts with the first interval.</summary>
    public double? MeanIntervalSeconds { get; private set; }

    /// <summary>The measured mean square of <c>FastMean - SlowMean</c>. It is the scale of the drift z value, so
    /// the correlation of the samples (a sine, a slow wave) never gives a false drift.</summary>
    public double DriftSpread { get; private set; }

    /// <summary>The number of samples that the plain mean of <see cref="DriftSpread"/> has seen.</summary>
    public int DriftSpreadCount { get; private set; }

    public double FastMean { get; private set; }

    public double Level { get; private set; }

    /// <summary>The slope of the trend, per second.</summary>
    public double Slope { get; private set; }

    public DateTime? LastSampledAt { get; private set; }

    public int SamplesSinceRestart { get; private set; }

    /// <summary>
    /// Tells whether the statistics are warm and the tracker has seen as many samples as the warm-up needs. The
    /// drift and trend rules wait for it.
    /// </summary>
    public bool IsReady(StatisticsSettings settings) => IsWarm && SamplesSinceRestart >= settings.WarmUpSamples;

    /// <summary>
    /// Tells whether the drift spread has its own warm-up of <c>WarmUpSamples</c> samples after the baseline
    /// warm-up. The drift rule waits for it.
    /// </summary>
    /// <param name="settings">The values of the detection options.</param>
    /// <returns><see langword="true"/> when the drift rule may judge.</returns>
    public bool IsDriftReady(StatisticsSettings settings) =>
        IsReady(settings) && DriftSpreadCount >= settings.WarmUpSamples;

    /// <summary>
    /// Learns one sample. A sample that is not after the last sample changes nothing.
    /// </summary>
    /// <param name="x">The value.</param>
    /// <param name="t">The <c>sampled_at</c> time of the value.</param>
    /// <param name="isFrozen">Tells whether an open episode of the metric freezes the baseline.</param>
    /// <param name="behavior">The behavior of the metric. A counter restarts the tracker when its value drops.</param>
    /// <param name="settings">The values of the detection options.</param>
    /// <returns>What the call did.</returns>
    public StatisticsUpdate Apply(
        double x,
        DateTime t,
        bool isFrozen,
        MetricBehavior behavior,
        StatisticsSettings settings
    )
    {
        var deltaSeconds = LastSampledAt is { } last ? (t - last).TotalSeconds : (double?)null;

        if (deltaSeconds is <= 0)
        {
            return StatisticsUpdate.Ignored;
        }

        MeanIntervalSeconds ??= deltaSeconds;

        var isRestarted = deltaSeconds > settings.GapSeconds
            || (behavior == MetricBehavior.Counter && IsWarm && x < Level);

        if (isRestarted)
        {
            RestartTracker(x);
        }

        if (!IsWarm)
        {
            LearnWarmUp(x, settings);
        }
        else if (!isRestarted && deltaSeconds is { } seconds)
        {
            LearnAfterWarmUp(x, seconds, isFrozen, settings);
        }

        SamplesSinceRestart++;
        LastSampledAt = t;

        return new StatisticsUpdate { IsIgnored = false, IsTrackerRestarted = isRestarted };
    }

    /// <summary>
    /// Calculates the sigma of the baseline. The floor is a share of the metric range, so a flat signal never gives a
    /// sigma of 0 when the metric has a range.
    /// </summary>
    /// <param name="min">The lower bound of the metric, or <see langword="null"/>.</param>
    /// <param name="max">The upper bound of the metric, or <see langword="null"/>.</param>
    /// <param name="settings">The values of the detection options.</param>
    /// <returns>The sigma.</returns>
    public double Sigma(double? min, double? max, StatisticsSettings settings)
    {
        var sigma = Math.Sqrt(Variance);

        return min is { } lower && max is { } upper
            ? Math.Max(sigma, settings.SigmaFloorShare * (upper - lower))
            : sigma;
    }

    /// <summary>
    /// Calculates the scale of the fast mean: the measured drift spread, never less than the white noise value of the
    /// sigma. The measured spread covers the correlation of real samples (a sine, a slow wave).
    /// </summary>
    /// <param name="min">The lower bound of the metric, or <see langword="null"/>.</param>
    /// <param name="max">The upper bound of the metric, or <see langword="null"/>.</param>
    /// <param name="settings">The values of the detection options.</param>
    /// <returns>The scale, or <see langword="null"/> before the first sample interval is known.</returns>
    public double? SigmaFast(double? min, double? max, StatisticsSettings settings)
    {
        if (MeanIntervalSeconds is not { } interval)
        {
            return null;
        }

        var lambda = 1 - Math.Exp(-interval / settings.FastTauSeconds);
        var whiteNoiseSigmaFast = Sigma(min, max, settings) * Math.Sqrt(lambda / (Two - lambda));

        return Math.Max(Math.Sqrt(DriftSpread), whiteNoiseSigmaFast);
    }

    /// <summary>
    /// Calculates the z value of the fast mean against the baseline.
    /// </summary>
    /// <param name="min">The lower bound of the metric, or <see langword="null"/>.</param>
    /// <param name="max">The upper bound of the metric, or <see langword="null"/>.</param>
    /// <param name="settings">The values of the detection options.</param>
    /// <returns>The z value, or <see langword="null"/> while no scale is known.</returns>
    public double? Z(double? min, double? max, StatisticsSettings settings) =>
        SigmaFast(min, max, settings) is > 0 and { } sigmaFast ? (FastMean - SlowMean) / sigmaFast : null;

    /// <summary>
    /// Calculates the time until the level reaches the alarm limit in the direction of the slope.
    /// </summary>
    /// <param name="alarmLow">The low alarm limit, or <see langword="null"/>.</param>
    /// <param name="alarmHigh">The high alarm limit, or <see langword="null"/>.</param>
    /// <returns>The seconds until the limit (negative when the level is beyond it), or <see langword="null"/>
    /// without a limit in the slope direction or without a slope.</returns>
    public double? ForecastSeconds(double? alarmLow, double? alarmHigh)
    {
        var limit = Slope switch
        {
            > 0 => alarmHigh,
            < 0 => alarmLow,
            _ => null,
        };

        return limit is { } theta ? (theta - Level) / Slope : null;
    }

    private void RestartTracker(double x)
    {
        FastMean = x;
        Level = x;
        Slope = 0;
        SamplesSinceRestart = 0;
    }

    private void LearnWarmUp(double x, StatisticsSettings settings)
    {
        WarmCount++;
        var delta = x - WarmMean;
        WarmMean += delta / WarmCount;
        WarmM2 += delta * (x - WarmMean);

        if (WarmCount < settings.WarmUpSamples)
        {
            return;
        }

        SlowMean = WarmMean;
        Variance = WarmCount > 1 ? WarmM2 / (WarmCount - 1) : 0;
        FastMean = SlowMean;
        Level = x;
        Slope = 0;
        IsWarm = true;
    }

    private void LearnAfterWarmUp(double x, double deltaSeconds, bool isFrozen, StatisticsSettings settings)
    {
        FastMean += Alpha(deltaSeconds, settings.FastTauSeconds) * (x - FastMean);

        if (!isFrozen)
        {
            var alphaSlow = Alpha(deltaSeconds, settings.SlowTauSeconds);
            var deviation = x - SlowMean;
            Variance = (1 - alphaSlow) * (Variance + alphaSlow * deviation * deviation);
            SlowMean += alphaSlow * deviation;
            MeanIntervalSeconds += alphaSlow * (deltaSeconds - MeanIntervalSeconds);
            LearnDriftSpread(alphaSlow, settings);
        }

        var predicted = Level + Slope * deltaSeconds;
        var newLevel = predicted + Alpha(deltaSeconds, settings.LevelTauSeconds) * (x - predicted);
        Slope += Alpha(deltaSeconds, settings.TrendTauSeconds) * ((newLevel - Level) / deltaSeconds - Slope);
        Level = newLevel;
    }

    private void LearnDriftSpread(double alphaSlow, StatisticsSettings settings)
    {
        var squared = (FastMean - SlowMean) * (FastMean - SlowMean);

        if (DriftSpreadCount < settings.WarmUpSamples)
        {
            DriftSpreadCount++;
            DriftSpread += (squared - DriftSpread) / DriftSpreadCount;
        }
        else
        {
            DriftSpread += alphaSlow * (squared - DriftSpread);
        }
    }

    private static double Alpha(double deltaSeconds, double tauSeconds) => 1 - Math.Exp(-deltaSeconds / tauSeconds);
}
