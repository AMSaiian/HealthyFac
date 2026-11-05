using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Messaging.Receiving;

/// <summary>
/// Handles pending inbox rows. Each message runs in its own scope, and <see cref="InboxMessageRunner"/> does the
/// work of one message in its own transaction. A failure rolls that unit back, and a fresh scope records the
/// attempt and the next attempt time, because the scope that failed still tracks the rolled back changes. At
/// <see cref="MessagingOptions.InboxMaxAttemptCount"/> attempts the row gets the failed state, the processor
/// stops claiming it, and one <c>Error</c> log entry reports it. A permanent failure (an invalid message) gets the
/// failed state at the first attempt. A run ends when no row is due. A failed row gets
/// a later attempt time or the failed state, so the same run cannot claim it again.
/// </summary>
/// <param name="scopeFactory">Creates the scopes of each message.</param>
/// <param name="timeProvider">The clock for the due time of the rows.</param>
/// <param name="options">The polling interval.</param>
/// <param name="serviceOptions">The name of the running service, for the actor that records a failure.</param>
/// <param name="metrics">Records the run duration and counts the retried and the failed messages.</param>
/// <param name="logger">The logger of the processor.</param>
internal sealed class InboxProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<MessagingOptions> options,
    IOptions<ServiceOptions> serviceOptions,
    MessagingMetrics metrics,
    ILogger<InboxProcessor> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<MessagingOptions> _options = options;
    private readonly IOptions<ServiceOptions> _serviceOptions = serviceOptions;
    private readonly MessagingMetrics _metrics = metrics;
    private readonly ILogger<InboxProcessor> _logger = logger;

    /// <summary>
    /// Handles pending rows until none is due. The run duration is recorded with its outcome; a cancelled run
    /// records nothing, because a cancellation is not a failure.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the run ends.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetTimestamp();
        try
        {
            bool isClaimed;
            do
            {
                isClaimed = await ProcessNextAsync(cancellationToken);
            }
            while (isClaimed);
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
            _logger.LogError(exception, "The inbox processor run failed. The next run starts after the interval.");
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        Guid? claimedId = null;

        try
        {
            return await RunNextAsync(messageId => claimedId = messageId, cancellationToken);
        }
        catch (Exception exception) when (claimedId is not null && !cancellationToken.IsCancellationRequested)
        {
            await RecordFailureAsync(claimedId.Value, exception, cancellationToken);

            return true;
        }
    }

    private async Task<bool> RunNextAsync(Action<Guid> onClaimed, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<InboxMessageRunner>()
            .RunNextAsync(onClaimed, cancellationToken);
    }

    /// <summary>
    /// Records the failure of a claimed row in a fresh scope. One save is atomic, and the execution strategy
    /// retries it, so no explicit transaction is needed. Another processor can claim the row after the failed
    /// scope rolled back its lock. Its change then fails the concurrency check here, and the processor skips the
    /// row.
    /// </summary>
    /// <param name="messageId">The ID of the claimed row.</param>
    /// <param name="exception">The failure of the handling attempt.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the failure is recorded or skipped.</returns>
    private async Task RecordFailureAsync(Guid messageId, Exception exception, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateActorScope(
            ServiceActor.InboxProcessor.For(_serviceOptions.Value.ToServiceName()));
        var dbContext = scope.ServiceProvider.GetRequiredService<IInboxDbContext>();

        var message = await dbContext.InboxMessages.SingleOrDefaultAsync(
            stored => stored.Id == messageId && stored.ProcessedOn == null && stored.FailedOn == null,
            cancellationToken);
        if (message is null)
        {
            _logger.LogWarning(exception, "Handling inbox message {MessageId} failed.", messageId);

            return;
        }

        if (IsPermanent(exception))
        {
            message.RecordPermanentFailure(_timeProvider.GetUtcNow().UtcDateTime, exception.Message);
        }
        else
        {
            message.RecordFailure(
                _timeProvider.GetUtcNow().UtcDateTime,
                exception.Message,
                _options.Value.InboxMaxAttemptCount);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogInformation(
                "Inbox message {MessageId} changed in another processor before its failure was recorded. "
                + "The processor skips it.",
                messageId);

            return;
        }

        LogFailure(message, exception);
        CountFailure(message, exception);
    }

    private void LogFailure(InboxMessage message, Exception exception)
    {
        if (IsPermanent(exception))
        {
            _logger.LogError(
                exception,
                "Inbox message {MessageId} of type {MessageType} is invalid. The processor does not retry it.",
                message.Id,
                message.Type);

            return;
        }

        if (message.FailedOn is null)
        {
            _logger.LogWarning(exception, "Handling inbox message {MessageId} failed.", message.Id);

            return;
        }

        _logger.LogError(
            exception,
            "Inbox message {MessageId} of type {MessageType} failed {AttemptCount} times. "
            + "The processor stops retrying it.",
            message.Id,
            message.Type,
            message.AttemptCount);
    }

    /// <summary>
    /// Counts the recorded failure: a permanent failure and a failure at the attempt limit end the row, every other
    /// failure schedules a retry.
    /// </summary>
    /// <param name="message">The row with its recorded failure.</param>
    /// <param name="exception">The failure of the handling attempt.</param>
    private void CountFailure(InboxMessage message, Exception exception)
    {
        if (IsPermanent(exception))
        {
            _metrics.RecordFailed(message.Type, ObservabilityConstants.Messaging.Failures.Permanent);

            return;
        }

        if (message.FailedOn is not null)
        {
            _metrics.RecordFailed(message.Type, ObservabilityConstants.Messaging.Failures.AttemptLimit);

            return;
        }

        _metrics.RecordRetried(ObservabilityConstants.Messaging.Processors.Inbox, message.Type);
    }

    private void RecordRun(string outcome, long startedAt) =>
        _metrics.RecordRun(
            ObservabilityConstants.Messaging.Processors.Inbox,
            outcome,
            _timeProvider.GetElapsedTime(startedAt));

    /// <summary>
    /// Tells a failure that no retry can fix from one that a later attempt can fix. A payload that does not
    /// deserialize and a request that fails stateless validation are permanent. Every other rejection (e.g., a
    /// missing entity) keeps the normal retries, because the order of messages is not guaranteed and the entity can
    /// appear later.
    /// </summary>
    /// <param name="exception">The failure of the handling attempt.</param>
    /// <returns><see langword="true"/> when the processor must not retry the row.</returns>
    private static bool IsPermanent(Exception exception) =>
        exception is InvalidIntegrationMessageException or AppValidationException;
}
