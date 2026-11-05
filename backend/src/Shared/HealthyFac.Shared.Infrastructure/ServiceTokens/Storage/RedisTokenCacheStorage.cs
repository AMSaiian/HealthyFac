using HealthyFac.Shared.Infrastructure.Caching;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;

/// <summary>
/// Keeps the token entries and the fetch locks in Redis through the shared <see cref="IConnectionMultiplexer"/>.
/// </summary>
/// <remarks>
/// Each call goes through the shared <see cref="RedisResiliencePipeline"/>. While its breaker is open, a call throws
/// <see cref="TokenStorageException"/> at once, so a Redis that is down does not slow every token request. A Redis
/// error or timeout counts as a failure of the breaker. The lock
/// release is one Lua script, because a plain delete could remove the lock of another holder after the own lock
/// ended.
/// </remarks>
/// <param name="connection">The shared Redis connection.</param>
/// <param name="redis">The shared Redis pipeline that protects the callers from a slow Redis.</param>
internal sealed class RedisTokenCacheStorage(IConnectionMultiplexer connection, RedisResiliencePipeline redis)
    : ITokenCacheStorage
{
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IConnectionMultiplexer _connection = connection;
    private readonly RedisResiliencePipeline _redis = redis;

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await RunAsync(database => database.StringGetAsync(key), cancellationToken);

        return value.IsNull ? null : value.ToString();
    }

    public async Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        await RunAsync(database => database.StringSetAsync(key, value, timeToLive), cancellationToken);
    }

    public async Task<bool> TryAcquireLockAsync(
        string key,
        string owner,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    )
    {
        var isAcquired = await RunAsync(
            database => database.StringSetAsync(key, owner, timeToLive, When.NotExists),
            cancellationToken);

        return isAcquired;
    }

    public async Task ReleaseLockAsync(string key, string owner, CancellationToken cancellationToken)
    {
        await RunAsync(
            database => database.ScriptEvaluateAsync(ReleaseScript, [(RedisKey)key], [(RedisValue)owner]),
            cancellationToken);
    }

    private async Task<TResult> RunAsync<TResult>(
        Func<IDatabase, Task<TResult>> operation,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _redis.ExecuteAsync(
                token => new ValueTask<TResult>(operation(_connection.GetDatabase()).WaitAsync(token)),
                cancellationToken);

            return result;
        }
        catch (BrokenCircuitException)
        {
            throw ServiceTokenErrors.StorageSkipped();
        }
        catch (Exception exception) when (exception is RedisException or RedisTimeoutException)
        {
            throw ServiceTokenErrors.StorageFailed(exception);
        }
    }
}
