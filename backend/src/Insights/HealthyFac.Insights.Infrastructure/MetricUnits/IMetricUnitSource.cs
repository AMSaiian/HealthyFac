using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Fetches the unit catalog from Telemetry. The Redis catalog calls it on a cache miss only.
/// </summary>
public interface IMetricUnitSource
{
    /// <summary>
    /// Fetches the catalog.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The units, sorted by quantity kind and unit name.</returns>
    Task<IReadOnlyList<MetricUnit>> FetchAsync(CancellationToken cancellationToken);
}
