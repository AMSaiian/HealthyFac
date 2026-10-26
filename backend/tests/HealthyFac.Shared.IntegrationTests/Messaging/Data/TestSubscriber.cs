using System.Collections.Concurrent;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization.Json;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public sealed record ReceivedMessage(
    Guid Id,
    string Type,
    string Payload,
    IReadOnlyDictionary<string, string> Headers
);

/// <summary>
/// Plays the part of another service that consumes topics. It owns a queue on the test broker, records every
/// envelope that arrives, and lets a test wait for a number of deliveries.
/// </summary>
public sealed class TestSubscriber : IDisposable
{
    private readonly ConcurrentQueue<ReceivedMessage> _received = new();

    private readonly BuiltinHandlerActivator _activator = new();

    private readonly IBus _bus;

    public TestSubscriber(string connectionString, string queueName)
    {
        _activator.Handle<IntegrationEnvelope>((_, context, message) =>
        {
            _received.Enqueue(
                new ReceivedMessage(
                    Guid.Parse(context.Headers[Headers.MessageId]),
                    message.Type,
                    message.Payload,
                    new Dictionary<string, string>(context.Headers)));

            return Task.CompletedTask;
        });

        _bus = Configure.With(_activator)
            .Transport(transport => transport.UseRabbitMq(connectionString, queueName))
            .Serialization(serialization => serialization.UseSystemTextJson())
            .Start();
    }

    public Task SubscribeAsync(string topic) => _bus.Advanced.Topics.Subscribe(topic);

    public List<ReceivedMessage> ReceivedOfType(string messageType)
        => _received.Where(message => message.Type == messageType).ToList();

    /// <summary>
    /// Waits until at least <paramref name="count"/> messages of the type arrived, then waits a quiet period,
    /// so a late duplicate would show in the result.
    /// </summary>
    /// <returns>The messages of the type that arrived by the end of the quiet period.</returns>
    public async Task<List<ReceivedMessage>> WaitForMessagesAsync(string messageType, int count)
    {
        using var cancellation = new CancellationTokenSource(TransportTestData.WaitTimeout);
        while (!cancellation.IsCancellationRequested && ReceivedOfType(messageType).Count < count)
        {
            await Task.Delay(TransportTestData.PollDelay);
        }

        await Task.Delay(TransportTestData.QuietPeriod);

        return ReceivedOfType(messageType);
    }

    public void Dispose() => _bus.Dispose();
}
