namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Builds the Redis keys of the unit catalog. Every key starts with the service name.
/// </summary>
public static class CacheKeys
{
    private const string Catalog = "metric-units";
    private const string LockSuffix = "lock";

    /// <summary>
    /// Builds the key of the cached catalog.
    /// </summary>
    /// <param name="serviceName">The name of the running service.</param>
    /// <returns>The key.</returns>
    public static string ForCatalog(string serviceName) => $"{serviceName}:{Catalog}";

    /// <summary>
    /// Builds the key of the fetch lock of the catalog.
    /// </summary>
    /// <param name="serviceName">The name of the running service.</param>
    /// <returns>The key.</returns>
    public static string ForCatalogLock(string serviceName) => $"{ForCatalog(serviceName)}:{LockSuffix}";
}
