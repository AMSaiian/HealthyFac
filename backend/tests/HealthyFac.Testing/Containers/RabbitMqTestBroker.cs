using RabbitMQ.Client;
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

    /// <summary>
    /// Removes the ready messages from the named queues and keeps the queues and their bindings, so the topology
    /// that Rebus declared at start stays. A queue that does not exist fails the call.
    /// </summary>
    /// <param name="queueNames">
    /// The queues of the fixture topology, for example the input queue and the error queue.
    /// </param>
    public async Task PurgeQueuesAsync(params string[] queueNames)
    {
        await using var connection = await new ConnectionFactory { Uri = new Uri(ConnectionString) }
            .CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        foreach (var queueName in queueNames)
        {
            await channel.QueuePurgeAsync(queueName);
        }
    }

    public Task StopAsync() => _container.StopAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
