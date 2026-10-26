using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// Publishes pending outbox rows to RabbitMQ. Each message runs in its own scope as the system actor, and
/// <see cref="OutboxMessagePublisher"/> does the work of one message in its own transaction. A run ends when no
/// row is due or when a publish fails, because the broker is then likely down and the other rows would fail
/// too. The next run starts after the polling interval.
/// </summary>
/// <param name="scopeFactory">Creates the scope of each message.</param>
/// <param name="options">The polling interval.</param>
/// <param name="logger">The logger of the processor.</param>
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<OutboxProcessor> logger
) : BackgroundService
{
    /// <summary>
    /// Publishes pending rows until none is due or a publish fails.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the run ends.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ClaimResult result;
        do
        {
            result = await PublishNextAsync(cancellationToken);
        }
        while (result is ClaimResult.Published);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSafelyAsync(stoppingToken);
            await Task.Delay(options.Value.PollingIntervalMilliseconds, stoppingToken);
        }
    }

    private async Task RunSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(exception, "The outbox processor run failed. The next run starts after the interval.");
        }
    }

    private async Task<ClaimResult> PublishNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateActorScope(SystemActor.Create());

        return await scope.ServiceProvider.GetRequiredService<OutboxMessagePublisher>()
            .PublishNextAsync(cancellationToken);
    }
}
