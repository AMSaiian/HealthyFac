using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Messaging.Sending;

/// <summary>
/// Publishes pending outbox rows to RabbitMQ. Each message runs in its own scope as the outbox processor actor of the
/// service, and <see cref="OutboxMessagePublisher"/> does the work of one message in its own transaction. A run ends
/// when no row is due or when a publish fails, because the broker is then likely down and the other rows would fail
/// too. The next run starts after the polling interval.
/// </summary>
/// <param name="scopeFactory">Creates the scope of each message.</param>
/// <param name="options">The polling interval.</param>
/// <param name="serviceOptions">The name of the running service, for the actor of each scope.</param>
/// <param name="metrics">Records the duration of each run.</param>
/// <param name="timeProvider">The clock for the run duration.</param>
/// <param name="logger">The logger of the processor.</param>
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    IOptions<ServiceOptions> serviceOptions,
    MessagingMetrics metrics,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IOptions<MessagingOptions> _options = options;
    private readonly IOptions<ServiceOptions> _serviceOptions = serviceOptions;
    private readonly MessagingMetrics _metrics = metrics;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<OutboxProcessor> _logger = logger;

    /// <summary>
    /// Publishes pending rows until none is due or a publish fails. The run duration is recorded with its outcome;
    /// a cancelled run records nothing, because a cancellation is not a failure.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the run ends.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetTimestamp();
        try
        {
            ClaimResult result;
            do
            {
                result = await PublishNextAsync(cancellationToken);
            }
            while (result is ClaimResult.Published);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            RecordRun(ObservabilityConstants.Outcomes.Failure, startedAt);

            throw;
        }

        RecordRun(ObservabilityConstants.Outcomes.Success, startedAt);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSafelyAsync(stoppingToken);
            await Task.Delay(_options.Value.PollingIntervalMilliseconds, stoppingToken);
        }
    }

    private async Task RunSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "The outbox processor run failed. The next run starts after the interval.");
        }
    }

    private async Task<ClaimResult> PublishNextAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(
            ServiceActor.OutboxProcessor.For(_serviceOptions.Value.ToServiceName()));

        return await scope.ServiceProvider.GetRequiredService<OutboxMessagePublisher>()
            .PublishNextAsync(cancellationToken);
    }

    private void RecordRun(string outcome, long startedAt) =>
        _metrics.RecordRun(
            ObservabilityConstants.Messaging.Processors.Outbox,
            outcome,
            _timeProvider.GetElapsedTime(startedAt));
}
