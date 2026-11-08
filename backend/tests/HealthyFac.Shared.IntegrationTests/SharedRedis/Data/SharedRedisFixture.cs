using HealthyFac.Shared.Infrastructure.Revocation;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using HealthyFac.Testing.Caching;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Resetting;
using StackExchange.Redis;

namespace HealthyFac.Shared.IntegrationTests.SharedRedis.Data;

/// <summary>
/// One Redis container for the token cache storage and the token revocation store. Both use their own key prefixes,
/// and the reset deletes every key before each test.
/// </summary>
public sealed class SharedRedisFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly RedisTestServer _redis = new();

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public ITokenCacheStorage Storage { get; private set; } = null!;

    public ITokenRevocationStore Store { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString);
        Storage = new RedisTokenCacheStorage(Connection, TestRedisPipeline.Create(TimeProvider.System));
        Store = new RedisTokenRevocationStore(Connection);
    }

    public async ValueTask ResetAsync() => await _redis.FlushAsync();

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
