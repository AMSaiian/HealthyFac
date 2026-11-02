using Testcontainers.Redis;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a Redis container for integration tests, with the image from <see cref="TestImages"/>.
/// <see cref="StopAsync"/> lets a test simulate a Redis outage.
/// </summary>
public sealed class RedisTestServer : IAsyncDisposable
{
    private readonly RedisContainer _container = new RedisBuilder(TestImages.Redis).Build();

    /// <summary>
    /// The connection string of the container, read once at start. It stays available after <see cref="StopAsync"/>,
    /// because the container then drops its port mapping, and a test needs the address of the stopped server.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
    }

    public Task StopAsync() => _container.StopAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
