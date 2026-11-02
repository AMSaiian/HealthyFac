using HealthyFac.Shared.Infrastructure.Revocation;
using HealthyFac.Testing.Containers;
using StackExchange.Redis;

namespace HealthyFac.Shared.IntegrationTests.Revocation.Data;

public sealed class RevocationStoreFixture : IAsyncLifetime
{
    private readonly RedisTestServer _redis = new();

    private IConnectionMultiplexer _connection = null!;

    public ITokenRevocationStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();

        _connection = await ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString);
        Store = new RedisTokenRevocationStore(_connection);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
