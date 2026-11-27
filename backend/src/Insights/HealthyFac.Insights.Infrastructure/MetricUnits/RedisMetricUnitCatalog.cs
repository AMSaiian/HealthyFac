using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Keeps a copy of the unit catalog in Redis, so an ingest run reads units without a call to Telemetry. The copy has a
/// time to live with a random jitter, and a change message deletes it. <see cref="RedisFetchCache"/> holds the fetch
/// and lock logic.
/// </summary>
/// <param name="cache">Fetches and stores the catalog.</param>
/// <param name="source">Fetches the catalog from Telemetry.</param>
/// <param name="cacheOptions">Holds the limits of the caches.</param>
/// <param name="serviceOptions">Holds the service name for the keys.</param>
public sealed class RedisMetricUnitCatalog(
    RedisFetchCache cache,
    IMetricUnitSource source,
    IOptions<InsightsCacheOptions> cacheOptions,
    IOptions<ServiceOptions> serviceOptions
) : IMetricUnitCatalog, IMetricUnitCache
{
    private const string CacheName = "unit catalog";

    private readonly RedisFetchCache _cache = cache;
    private readonly IMetricUnitSource _source = source;
    private readonly RedisFetchCacheOptions _options = cacheOptions.Value.MetricUnits;
    private readonly string _entryKey = CacheKeys.MetricUnits.Entry(serviceOptions.Value.Name);
    private readonly string _lockKey = CacheKeys.MetricUnits.Lock(serviceOptions.Value.Name);

    public Task<IReadOnlyList<MetricUnit>> GetAllAsync(CancellationToken cancellationToken) =>
        _cache.GetOrFetchAsync(CacheName, _entryKey, _lockKey, _options, _source.FetchAsync, cancellationToken);

    public Task ForgetAsync(CancellationToken cancellationToken) =>
        _cache.ForgetAsync(CacheName, _entryKey, cancellationToken);
}
