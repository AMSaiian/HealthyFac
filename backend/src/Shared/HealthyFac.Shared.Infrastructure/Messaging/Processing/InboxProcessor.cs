using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

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
/// <param name="logger">The logger of the processor.</param>
internal sealed class InboxProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<MessagingOptions> options,
    ILogger<InboxProcessor> logger
) : BackgroundService
{
    /// <summary>
    /// Handles pending rows until none is due.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the run ends.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        bool isClaimed;
        do
        {
            isClaimed = await ProcessNextAsync(cancellationToken);
        }
        while (isClaimed);
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
            logger.LogError(exception, "The inbox processor run failed. The next run starts after the interval.");
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
        using var scope = scopeFactory.CreateScope();

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
        using var scope = scopeFactory.CreateActorScope(SystemActor.Create());
        var dbContext = scope.ServiceProvider.GetRequiredService<IInboxDbContext>();

        var message = await dbContext.InboxMessages.SingleOrDefaultAsync(
            stored => stored.Id == messageId && stored.ProcessedOn == null && stored.FailedOn == null,
            cancellationToken);
        if (message is null)
        {
            logger.LogWarning(exception, "Handling inbox message {MessageId} failed.", messageId);

            return;
        }

        if (IsPermanent(exception))
        {
            message.RecordPermanentFailure(timeProvider.GetUtcNow().UtcDateTime, exception.Message);
        }
        else
        {
            message.RecordFailure(
                timeProvider.GetUtcNow().UtcDateTime,
                exception.Message,
                options.Value.InboxMaxAttemptCount);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation(
                "Inbox message {MessageId} changed in another processor before its failure was recorded. "
                + "The processor skips it.",
                messageId);

            return;
        }

        LogFailure(message, exception);
    }

    private void LogFailure(InboxMessage message, Exception exception)
    {
        if (IsPermanent(exception))
        {
            logger.LogError(
                exception,
                "Inbox message {MessageId} of type {MessageType} is invalid. The processor does not retry it.",
                message.Id,
                message.Type);

            return;
        }

        if (message.FailedOn is null)
        {
            logger.LogWarning(exception, "Handling inbox message {MessageId} failed.", message.Id);

            return;
        }

        logger.LogError(
            exception,
            "Inbox message {MessageId} of type {MessageType} failed {AttemptCount} times. "
            + "The processor stops retrying it.",
            message.Id,
            message.Type,
            message.AttemptCount);
    }

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
