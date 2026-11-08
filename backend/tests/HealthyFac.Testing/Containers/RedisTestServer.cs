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

    /// <summary>
    /// Deletes every key of the database (<c>FLUSHDB</c>), so a test starts without the cache and counter entries of an
    /// earlier test.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <c>redis-cli</c> exits with a non-zero code.</exception>
    public async Task FlushAsync()
    {
        var result = await _container.ExecAsync(["redis-cli", "FLUSHDB"]);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"FLUSHDB exited with code {result.ExitCode}: {result.Stderr}");
        }
    }

    public Task StopAsync() => _container.StopAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
