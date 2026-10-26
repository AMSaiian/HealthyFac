using Testcontainers.RabbitMq;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a RabbitMQ container for integration tests, with the image from <see cref="TestImages"/>.
/// <see cref="StopAsync"/> lets a test simulate a broker outage.
/// </summary>
public sealed class RabbitMqTestBroker : IAsyncDisposable
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder(TestImages.RabbitMq).Build();

    /// <summary>
    /// The AMQP URI of the container, read once at start. It stays available after <see cref="StopAsync"/>,
    /// because the container then drops its port mapping, and a test needs the address of the stopped broker.
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
