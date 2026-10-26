using Microsoft.Extensions.Hosting;
using Rebus.Bus;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Subscribes the input queue to the topic of every registered message type when the host starts, so the broker
/// routes those messages to the queue. Register it after the bus, because the bus starts first.
/// </summary>
/// <param name="bus">The Rebus bus of the service.</param>
/// <param name="registry">The message types that the service handles.</param>
internal sealed class SubscriptionHostedService(IBus bus, IntegrationMessageRegistry registry) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var messageType in registry.MessageTypes)
        {
            await bus.Advanced.Topics.Subscribe(messageType);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
