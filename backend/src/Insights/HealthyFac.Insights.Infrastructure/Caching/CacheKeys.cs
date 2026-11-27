namespace HealthyFac.Insights.Infrastructure.Caching;

/// <summary>
/// Builds the Redis keys of the Insights caches. Every key starts with the service name.
/// </summary>
public static class CacheKeys
{
    private const string LockSuffix = "lock";

    /// <summary>The keys of the unit catalog.</summary>
    public static class MetricUnits
    {
        private const string Catalog = "metric-units";

        /// <summary>
        /// Builds the key of the cached catalog.
        /// </summary>
        /// <param name="serviceName">The name of the running service.</param>
        /// <returns>The key.</returns>
        public static string Entry(string serviceName) => $"{serviceName}:{Catalog}";

        /// <summary>
        /// Builds the key of the fetch lock of the catalog.
        /// </summary>
        /// <param name="serviceName">The name of the running service.</param>
        /// <returns>The key.</returns>
        public static string Lock(string serviceName) => $"{Entry(serviceName)}:{LockSuffix}";
    }

    /// <summary>The keys of the company time zones.</summary>
    public static class CompanyZones
    {
        private const string Zone = "company-zone";

        /// <summary>
        /// Builds the key of the cached time zone of one company.
        /// </summary>
        /// <param name="serviceName">The name of the running service.</param>
        /// <param name="companyId">The company.</param>
        /// <returns>The key.</returns>
        public static string Entry(string serviceName, Guid companyId) => $"{serviceName}:{Zone}:{companyId}";

        /// <summary>
        /// Builds the key of the fetch lock of the time zone of one company.
        /// </summary>
        /// <param name="serviceName">The name of the running service.</param>
        /// <param name="companyId">The company.</param>
        /// <returns>The key.</returns>
        public static string Lock(string serviceName, Guid companyId) =>
            $"{Entry(serviceName, companyId)}:{LockSuffix}";
    }
}
