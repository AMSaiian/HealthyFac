using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Infrastructure.Caching;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Chats.Reads;

/// <summary>
/// Reads the time zone of the company of the user from Redis, and from Core on a miss. The cache has one entry for each
/// company.
/// </summary>
/// <remarks>
/// No code deletes an entry, because <c>Company.TimeZoneId</c> never changes after the company is created. A failure of
/// Core passes through and stores nothing.
/// </remarks>
/// <param name="cache">Fetches and stores the zone.</param>
/// <param name="source">Reads the zone from Core.</param>
/// <param name="currentUser">Provides the company of the user.</param>
/// <param name="cacheOptions">Holds the limits of the caches.</param>
/// <param name="serviceOptions">Holds the service name for the keys.</param>
public sealed class RedisCompanyTimeZoneReader(
    RedisFetchCache cache,
    GrpcCompanyTimeZoneReader source,
    ICurrentUserProvider currentUser,
    IOptions<InsightsCacheOptions> cacheOptions,
    IOptions<ServiceOptions> serviceOptions
) : ICompanyTimeZoneReader
{
    private const string CacheName = "company time zone";

    private readonly RedisFetchCache _cache = cache;
    private readonly GrpcCompanyTimeZoneReader _source = source;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly RedisFetchCacheOptions _options = cacheOptions.Value.CompanyZones;
    private readonly string _serviceName = serviceOptions.Value.Name;

    public Task<string> ReadTimeZoneIdAsync(CancellationToken cancellationToken)
    {
        var companyId = _currentUser.GetCompanyId();

        return _cache.GetOrFetchAsync(
            CacheName,
            CacheKeys.CompanyZones.Entry(_serviceName, companyId),
            CacheKeys.CompanyZones.Lock(_serviceName, companyId),
            _options,
            _source.ReadTimeZoneIdAsync,
            cancellationToken);
    }
}
