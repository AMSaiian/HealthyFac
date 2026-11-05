using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Testing.Caching;
using HealthyFac.Testing.Containers;
using StackExchange.Redis;

namespace HealthyFac.Shared.IntegrationTests.ServiceTokens.Data;

public sealed class TokenStorageFixture : IAsyncLifetime
{
    private readonly RedisTestServer _redis = new();

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public ITokenCacheStorage Storage { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString);
        Storage = new RedisTokenCacheStorage(Connection, TestRedisPipeline.Create(TimeProvider.System));
    }

    public async Task DisposeAsync()
    {
        await Connection.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
