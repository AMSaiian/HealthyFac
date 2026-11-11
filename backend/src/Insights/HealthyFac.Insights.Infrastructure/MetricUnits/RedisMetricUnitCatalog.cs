using System.Text.Json;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Keeps a copy of the unit catalog in Redis, so an ingest run reads units without a call to Telemetry. The copy has a
/// time to live with a random jitter, and a change message deletes it.
/// </summary>
/// <remarks>
/// A missing entry is fetched by one call: it takes the Redis lock, reads the entry again, fetches from Telemetry,
/// writes the entry, and releases the lock. Every other call, in every instance, polls the entry until it appears or
/// the lock time ends, and then fetches itself. The class keeps no state in process memory. The copy is never the
/// source of truth: a <see cref="RedisException"/>, a <see cref="RedisTimeoutException"/>, or an open breaker
/// (<see cref="BrokenCircuitException"/>) logs one <c>Warning</c> and the call reads Telemetry. Every Redis call runs
/// through the shared Redis pipeline. The lock release is one script, because a plain delete could remove the lock of
/// another holder after the own lock ended.
/// </remarks>
/// <param name="connection">The shared Redis connection.</param>
/// <param name="redis">The shared Redis pipeline with the circuit breaker.</param>
/// <param name="source">Fetches the catalog from Telemetry.</param>
/// <param name="timeProvider">Provides the clock of the waits.</param>
/// <param name="cacheOptions">Holds the time to live, the jitter, and the lock time.</param>
/// <param name="serviceOptions">Holds the service name for the keys.</param>
/// <param name="logger">Logs the Redis failures.</param>
public sealed class RedisMetricUnitCatalog(
    IConnectionMultiplexer connection,
    RedisResiliencePipeline redis,
    IMetricUnitSource source,
    TimeProvider timeProvider,
    IOptions<MetricUnitCacheOptions> cacheOptions,
    IOptions<ServiceOptions> serviceOptions,
    ILogger<RedisMetricUnitCatalog> logger
) : IMetricUnitCatalog, IMetricUnitCache
{
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IConnectionMultiplexer _connection = connection;
    private readonly RedisResiliencePipeline _redis = redis;
    private readonly IMetricUnitSource _source = source;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly MetricUnitCacheOptions _options = cacheOptions.Value;
    private readonly string _entryKey = CacheKeys.ForCatalog(serviceOptions.Value.Name);
    private readonly string _lockKey = CacheKeys.ForCatalogLock(serviceOptions.Value.Name);
    private readonly ILogger<RedisMetricUnitCatalog> _logger = logger;

    public async Task<IReadOnlyList<MetricUnit>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cached = await ReadAsync(cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            return await FetchAcrossInstancesAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogRedisFailure(exception);

            return await _source.FetchAsync(cancellationToken);
        }
    }

    public async Task ForgetAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(database => database.KeyDeleteAsync(_entryKey), cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogRedisFailure(exception);
        }
    }

    private static bool IsRedisFailure(Exception exception) =>
        exception is RedisException or RedisTimeoutException or BrokenCircuitException;

    private async Task<IReadOnlyList<MetricUnit>> FetchAcrossInstancesAsync(CancellationToken cancellationToken)
    {
        var owner = Guid.NewGuid().ToString(format: "N");
        var isAcquired = await RunAsync(
            database => database.StringSetAsync(_lockKey, owner, _options.LockTimeout, When.NotExists),
            cancellationToken);

        if (isAcquired)
        {
            return await FetchHoldingLockAsync(owner, cancellationToken);
        }

        var waited = await WaitForOtherInstanceAsync(cancellationToken);

        return waited ?? await FetchAndStoreAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MetricUnit>> FetchHoldingLockAsync(
        string owner,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var cached = await ReadAsync(cancellationToken);

            return cached ?? await FetchAndStoreAsync(cancellationToken);
        }
        finally
        {
            await TryReleaseLockAsync(owner);
        }
    }

    private async Task<IReadOnlyList<MetricUnit>?> WaitForOtherInstanceAsync(CancellationToken cancellationToken)
    {
        var deadline = _timeProvider.GetUtcNow() + _options.LockTimeout;
        var pollInterval = TimeSpan.FromMilliseconds(_options.PollMilliseconds);

        while (_timeProvider.GetUtcNow() < deadline)
        {
            await Task.Delay(pollInterval, _timeProvider, cancellationToken);

            var cached = await ReadAsync(cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<MetricUnit>> FetchAndStoreAsync(CancellationToken cancellationToken)
    {
        var units = await _source.FetchAsync(cancellationToken);
        var jitter = Random.Shared.Next(_options.JitterSeconds + 1);
        var timeToLive = TimeSpan.FromSeconds(_options.TimeToLiveSeconds + jitter);
        var json = JsonSerializer.Serialize(units);

        await RunAsync(database => database.StringSetAsync(_entryKey, json, timeToLive), cancellationToken);

        return units;
    }

    private async Task<IReadOnlyList<MetricUnit>?> ReadAsync(CancellationToken cancellationToken)
    {
        var value = await RunAsync(database => database.StringGetAsync(_entryKey), cancellationToken);

        return value.IsNull ? null : JsonSerializer.Deserialize<List<MetricUnit>>(value.ToString());
    }

    private async Task TryReleaseLockAsync(string owner)
    {
        try
        {
            await RunAsync(
                database => database.ScriptEvaluateAsync(
                    ReleaseScript,
                    [(RedisKey)_lockKey],
                    [(RedisValue)owner]),
                CancellationToken.None);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogRedisFailure(exception);
        }
    }

    private async Task<TResult> RunAsync<TResult>(
        Func<IDatabase, Task<TResult>> operation,
        CancellationToken cancellationToken
    )
    {
        var result = await _redis.ExecuteAsync(
            token => new ValueTask<TResult>(operation(_connection.GetDatabase()).WaitAsync(token)),
            cancellationToken);

        return result;
    }

    private void LogRedisFailure(Exception exception) =>
        _logger.LogWarning(
            message: "The unit catalog cache is not available. The call reads Telemetry. Failure: {FailureMessage}",
            exception.Message);
}
