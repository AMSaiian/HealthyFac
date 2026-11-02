using System.Threading.RateLimiting;
using HealthyFac.Shared.Infrastructure.Caching;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Limits through Redis, so every gateway instance shares one counter, and falls back to a local limiter when
/// Redis fails. Redis is never a hard dependency of the gateway.
/// </summary>
/// <remarks>
/// While the Redis circuit breaker of <see cref="RedisResiliencePipeline"/> is open, only the local limiter answers.
/// The local limit applies to one gateway instance, so during an outage the effective limit is up to the number of
/// instances times the configured limit. The rate limiter middleware tries a synchronous acquire first and calls the
/// asynchronous acquire only when it fails. Redis has no synchronous path, so <see cref="AttemptAcquireCore"/>
/// denies without consuming a permit, and <see cref="AcquireAsyncCore"/> decides every request.
/// <para>
/// The local limiter exists only after the first fallback, so a healthy partition holds no local limiter and no
/// timer. The limiter does not replenish itself. The partitioned limiter calls <see cref="TryReplenish"/> on every
/// heartbeat, and the call reaches the local limiter only when it exists.
/// </para>
/// <para>
/// The partitioned limiter disposes a partition that is idle for too long. The idle time is the smaller value of
/// both limiters, and it is <c>null</c> while any of them has an active request. The Redis limiter reports
/// <c>null</c> during a Redis call, so the partition is never disposed while a request waits on Redis. Without
/// this rule the fallback after a Redis timeout would use a disposed local limiter. Before the local limiter
/// exists, the idle time is the idle time of the Redis limiter.
/// </para>
/// <para>
/// <see cref="GetStatistics"/> returns the statistics of the local limiter, or <c>null</c> before it exists.
/// </para>
/// </remarks>
/// <param name="primary">The Redis limiter.</param>
/// <param name="createLocal">Creates the in-memory limiter with the same limits. It must not replenish itself.</param>
/// <param name="window">The replenishment period of the limits.</param>
/// <param name="redis">The shared Redis pipeline that records Redis failures and fails fast while it is open.</param>
internal sealed class FallbackRateLimiter(
    RateLimiter primary,
    Func<ReplenishingRateLimiter> createLocal,
    TimeSpan window,
    RedisResiliencePipeline redis
) : ReplenishingRateLimiter
{
    private readonly RateLimiter _primary = primary;
    private readonly TimeSpan _window = window;
    private readonly RedisResiliencePipeline _redis = redis;

    private readonly Lazy<ReplenishingRateLimiter> _local =
        new(createLocal, LazyThreadSafetyMode.ExecutionAndPublication);

    public override bool IsAutoReplenishing => false;

    public override TimeSpan ReplenishmentPeriod => _window;

    public override TimeSpan? IdleDuration
    {
        get
        {
            var primaryIdleDuration = _primary.IdleDuration;
            if (!_local.IsValueCreated)
            {
                return primaryIdleDuration;
            }

            var localIdleDuration = _local.Value.IdleDuration;
            if (primaryIdleDuration is null || localIdleDuration is null)
            {
                return null;
            }

            return localIdleDuration < primaryIdleDuration ? localIdleDuration : primaryIdleDuration;
        }
    }

    public override RateLimiterStatistics? GetStatistics() =>
        _local.IsValueCreated ? _local.Value.GetStatistics() : null;

    public override bool TryReplenish() => _local.IsValueCreated && _local.Value.TryReplenish();

    protected override RateLimitLease AttemptAcquireCore(int permitCount) => DeniedRateLimitLease.Instance;

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var primaryLease = await _redis.ExecuteAsync(
                token => _primary.AcquireAsync(permitCount, token),
                cancellationToken);

            return primaryLease;
        }
        catch (Exception exception)
            when (exception is RedisException or RedisTimeoutException or BrokenCircuitException)
        {
            return await _local.Value.AcquireAsync(permitCount, cancellationToken);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _primary.Dispose();

            if (_local.IsValueCreated)
            {
                _local.Value.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _primary.DisposeAsync();

        if (_local.IsValueCreated)
        {
            await _local.Value.DisposeAsync();
        }
    }
}
