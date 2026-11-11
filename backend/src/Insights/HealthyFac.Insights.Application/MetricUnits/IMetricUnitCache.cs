namespace HealthyFac.Insights.Application.MetricUnits;

/// <summary>
/// Drops the cached copy of the unit catalog, so the next read calls Telemetry.
/// </summary>
public interface IMetricUnitCache
{
    /// <summary>
    /// Deletes the cached copy. A cache failure is logged and never thrown, because the next read then fetches again
    /// when the entry expires.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>A task that completes when the delete ends.</returns>
    Task ForgetAsync(CancellationToken cancellationToken);
}
