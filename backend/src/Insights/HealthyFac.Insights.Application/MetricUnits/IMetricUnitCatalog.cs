namespace HealthyFac.Insights.Application.MetricUnits;

/// <summary>
/// Reads the unit catalog of Telemetry. Infrastructure keeps a copy in Redis, so a read does not call Telemetry each
/// time.
/// </summary>
public interface IMetricUnitCatalog
{
    /// <summary>
    /// Reads every unit of the catalog.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The units, sorted by quantity kind and unit name.</returns>
    Task<IReadOnlyList<MetricUnit>> GetAllAsync(CancellationToken cancellationToken);
}
