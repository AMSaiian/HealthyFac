using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.UnitTests.Evaluation.Data;

/// <summary>
/// Builds the test series of the statistics and rule tests: noise around a level, a step, a ramp, uneven intervals,
/// and a gap. Every series uses a fixed seed.
/// </summary>
public static class SignalSeries
{
    public const double NoiseSigma = 1;

    public static readonly DateTime Start = new(
        year: 2026, month: 11, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly StatisticsSettings Settings = new()
    {
        WarmUpSamples = 60,
        SlowTauSeconds = 1800,
        FastTauSeconds = 20,
        LevelTauSeconds = 30,
        TrendTauSeconds = 60,
        GapSeconds = 600,
        SigmaFloorShare = 0.01,
    };

    public static IReadOnlyList<(DateTime Time, double Value)> Noisy(
        double mean,
        int count,
        DateTime start,
        double stepSeconds = 1,
        int seed = 7
    )
    {
        var random = new Random(seed);

        return Enumerable.Range(start: 0, count)
            .Select(index => (start.AddSeconds(index * stepSeconds), mean + NoiseSigma * NextGaussian(random)))
            .ToList();
    }

    /// <summary>A sine with noise: the slow, correlated shape of a loaded compressor.</summary>
    public static IReadOnlyList<(DateTime Time, double Value)> Sine(
        double mean,
        double amplitude,
        double periodSeconds,
        int count,
        DateTime start,
        double stepSeconds = 1,
        int seed = 11
    )
    {
        var random = new Random(seed);

        return Enumerable.Range(start: 0, count)
            .Select(index => (
                start.AddSeconds(index * stepSeconds),
                mean
                + amplitude * Math.Sin(2.0 * Math.PI * index * stepSeconds / periodSeconds)
                + NoiseSigma * NextGaussian(random)))
            .ToList();
    }

    public static IReadOnlyList<(DateTime Time, double Value)> Constant(
        double value,
        int count,
        DateTime start,
        double stepSeconds = 1
    ) =>
        Enumerable.Range(start: 0, count)
            .Select(index => (start.AddSeconds(index * stepSeconds), value))
            .ToList();

    public static IReadOnlyList<(DateTime Time, double Value)> Ramp(
        double from,
        double perSecond,
        int count,
        DateTime start,
        double stepSeconds = 1
    ) =>
        Enumerable.Range(start: 0, count)
            .Select(index => (start.AddSeconds(index * stepSeconds), from + perSecond * index * stepSeconds))
            .ToList();

    public static MetricStatistics Learn(IEnumerable<(DateTime Time, double Value)> series)
    {
        var statistics = new MetricStatistics(Guid.CreateVersion7(), operatingPhaseId: null);

        foreach (var (time, value) in series)
        {
            statistics.Apply(value, time, isFrozen: false, MetricBehavior.Level, Settings);
        }

        return statistics;
    }

    private static double NextGaussian(Random random)
    {
        var first = 1.0 - random.NextDouble();
        var second = random.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(first)) * Math.Cos(2.0 * Math.PI * second);
    }
}
