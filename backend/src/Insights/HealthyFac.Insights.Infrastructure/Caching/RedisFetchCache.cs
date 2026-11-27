using System.Text.Json;
using HealthyFac.Shared.Infrastructure.Caching;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Insights.Infrastructure.Caching;

/// <summary>
/// Keeps copies of fetched values in Redis. An entry has a time to live with a random jitter.
/// </summary>
/// <remarks>
/// A missing entry is fetched by one call: it takes the Redis lock, reads the entry again, fetches the value, writes
/// the entry, and releases the lock. Every other call, in every instance, polls the entry until it appears or the lock
/// time ends, and then fetches itself. The class keeps no state in process memory. The copy is never the source of
/// truth: a <see cref="RedisException"/>, a <see cref="RedisTimeoutException"/>, or an open breaker
/// (<see cref="BrokenCircuitException"/>) logs one <c>Warning</c> and the call fetches the value. Every Redis call runs
/// through the shared Redis pipeline. The lock release is one script, because a plain delete could remove the lock of
/// another holder after the own lock ended. A failed fetch stores nothing.
/// </remarks>
/// <param name="connection">The shared Redis connection.</param>
/// <param name="redis">The shared Redis pipeline with the circuit breaker.</param>
/// <param name="timeProvider">Provides the clock of the waits.</param>
/// <param name="logger">Logs the Redis failures.</param>
public sealed class RedisFetchCache(
    IConnectionMultiplexer connection,
    RedisResiliencePipeline redis,
    TimeProvider timeProvider,
    ILogger<RedisFetchCache> logger
)
{
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IConnectionMultiplexer _connection = connection;
    private readonly RedisResiliencePipeline _redis = redis;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<RedisFetchCache> _logger = logger;

    /// <summary>
    /// Reads the entry, or fetches the value across the instances and stores it.
    /// </summary>
    /// <param name="cacheName">The name of the cache, for the log.</param>
    /// <param name="entryKey">The key of the entry.</param>
    /// <param name="lockKey">The key of the fetch lock.</param>
    /// <param name="options">The limits of the cache.</param>
    /// <param name="fetch">Fetches the value from its source.</param>
    /// <param name="cancellationToken">The token that stops the call.</param>
    /// <typeparam name="T">The type of the value. It must serialize to JSON.</typeparam>
    /// <returns>The value.</returns>
    public async Task<T> GetOrFetchAsync<T>(
        string cacheName,
        string entryKey,
        string lockKey,
        RedisFetchCacheOptions options,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken
    )
        where T : class
    {
        try
        {
            var cached = await ReadAsync<T>(entryKey, cancellationToken);

            if (cached is not null)
            {
                return cached;
            }

            return await FetchAcrossInstancesAsync(entryKey, lockKey, options, fetch, cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogRedisFailure(cacheName, exception);

            return await fetch(cancellationToken);
        }
    }

    /// <summary>
    /// Deletes the entry. A Redis failure logs one <c>Warning</c>.
    /// </summary>
    /// <param name="cacheName">The name of the cache, for the log.</param>
    /// <param name="entryKey">The key of the entry.</param>
    /// <param name="cancellationToken">The token that stops the call.</param>
    public async Task ForgetAsync(string cacheName, string entryKey, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(database => database.KeyDeleteAsync(entryKey), cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            LogRedisFailure(cacheName, exception);
        }
    }

    private static bool IsRedisFailure(Exception exception) =>
        exception is RedisException or RedisTimeoutException or BrokenCircuitException;

    private async Task<T> FetchAcrossInstancesAsync<T>(
        string entryKey,
        string lockKey,
        RedisFetchCacheOptions options,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var owner = Guid.NewGuid().ToString(format: "N");

        var isAcquired = await RunAsync(
            database => database.StringSetAsync(lockKey, owner, options.LockTimeout, When.NotExists),
            cancellationToken);

        if (isAcquired)
        {
            return await FetchHoldingLockAsync(entryKey, lockKey, owner, options, fetch, cancellationToken);
        }

        var waited = await WaitForOtherInstanceAsync<T>(entryKey, options, cancellationToken);

        return waited ?? await FetchAndStoreAsync(entryKey, options, fetch, cancellationToken);
    }

    private async Task<T> FetchHoldingLockAsync<T>(
        string entryKey,
        string lockKey,
        string owner,
        RedisFetchCacheOptions options,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken
    )
        where T : class
    {
        try
        {
            var cached = await ReadAsync<T>(entryKey, cancellationToken);

            return cached ?? await FetchAndStoreAsync(entryKey, options, fetch, cancellationToken);
        }
        finally
        {
            await TryReleaseLockAsync(lockKey, owner);
        }
    }

    private async Task<T?> WaitForOtherInstanceAsync<T>(
        string entryKey,
        RedisFetchCacheOptions options,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var deadline = _timeProvider.GetUtcNow() + options.LockTimeout;
        var pollInterval = TimeSpan.FromMilliseconds(options.PollMilliseconds);

        while (_timeProvider.GetUtcNow() < deadline)
        {
            await Task.Delay(pollInterval, _timeProvider, cancellationToken);

            var cached = await ReadAsync<T>(entryKey, cancellationToken);

            if (cached is not null)
            {
                return cached;
            }
        }

        return default(T);
    }

    private async Task<T> FetchAndStoreAsync<T>(
        string entryKey,
        RedisFetchCacheOptions options,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var value = await fetch(cancellationToken);
        var jitter = Random.Shared.Next(options.JitterSeconds + 1);
        var timeToLive = TimeSpan.FromSeconds(options.TimeToLiveSeconds + jitter);
        var json = JsonSerializer.Serialize(value);

        await RunAsync(database => database.StringSetAsync(entryKey, json, timeToLive), cancellationToken);

        return value;
    }

    private async Task<T?> ReadAsync<T>(string entryKey, CancellationToken cancellationToken)
        where T : class
    {
        var value = await RunAsync(database => database.StringGetAsync(entryKey), cancellationToken);

        return value.IsNull ? default(T) : JsonSerializer.Deserialize<T>(value.ToString());
    }

    private async Task TryReleaseLockAsync(string lockKey, string owner)
    {
        try
        {
            await RunAsync(
                database => database.ScriptEvaluateAsync(
                    ReleaseScript,
                    [(RedisKey)lockKey],
                    [(RedisValue)owner]),
                CancellationToken.None);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            _logger.LogWarning(
                message: "A cache lock could not be released. Failure: {FailureMessage}",
                exception.Message);
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

    private void LogRedisFailure(string cacheName, Exception exception) =>
        _logger.LogWarning(
            message: "The {CacheName} cache is not available. The call reads the source. Failure: {FailureMessage}",
            cacheName,
            exception.Message);
}
