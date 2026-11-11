using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Infrastructure.MetricUnits;

namespace HealthyFac.Insights.IntegrationTests.MetricUnits.Data;

/// <summary>
/// Replaces the Telemetry client in the integration tests. It counts the fetches and can wait before it answers, so a
/// test makes parallel callers overlap.
/// </summary>
public sealed class FakeMetricUnitSource : IMetricUnitSource
{
    private int _fetchCount;

    public static IReadOnlyList<MetricUnit> Units { get; } =
    [
        new() { QuantityKind = "temperature", Name = "celsius", Abbreviation = "C" },
        new() { QuantityKind = "pressure", Name = "bar", Abbreviation = "bar" },
    ];

    public int FetchCount => _fetchCount;

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public void Reset()
    {
        Delay = TimeSpan.Zero;
        Interlocked.Exchange(ref _fetchCount, value: 0);
    }

    public async Task<IReadOnlyList<MetricUnit>> FetchAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _fetchCount);
        await Task.Delay(Delay, cancellationToken);

        return Units;
    }
}
