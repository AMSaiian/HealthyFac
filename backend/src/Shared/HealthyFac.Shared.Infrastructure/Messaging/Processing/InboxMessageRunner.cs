using System.Diagnostics;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// Handles one pending inbox row in its own transaction. The class is scoped, so the handler of the message
/// shares the service context of the scope that the inbox processor opens for the row. The runner locks the row,
/// enters the actor of the row, calls the typed handler, and marks the row. The handler changes and the processed
/// state commit together. A failure rolls the whole unit back.
/// </summary>
/// <param name="dbContext">The service context that holds the inbox set.</param>
/// <param name="inbox">The inbox that marks a row as processed.</param>
/// <param name="dispatcher">Calls the typed handler of the message.</param>
/// <param name="holder">The holder of the actor of the scope.</param>
/// <param name="timeProvider">The clock for the due time of the rows.</param>
/// <param name="logger">The logger of the runner.</param>
internal sealed class InboxMessageRunner(
    IInboxDbContext dbContext,
    IInbox inbox,
    IntegrationMessageDispatcher dispatcher,
    ActorContextHolder holder,
    TimeProvider timeProvider,
    ILogger<InboxMessageRunner> logger
)
{
    /// <summary>
    /// Claims the next due row and handles it. After the transaction commits, the runner logs the handled message
    /// at <c>Information</c>. A retried or rolled-back attempt never logs it.
    /// </summary>
    /// <param name="onClaimed">Receives the ID of the claimed row, so the processor can schedule a retry after a
    /// failure.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="true"/> when a row was handled, and <see langword="false"/> when no row is due.
    /// </returns>
    public async Task<bool> RunNextAsync(Action<Guid> onClaimed, CancellationToken cancellationToken)
    {
        var handled = await dbContext.RunInTransactionAsync<(Guid Id, string Type)?>(
            async token =>
            {
                var message = await dbContext.ClaimNextInboxMessageAsync(
                    timeProvider.GetUtcNow().UtcDateTime,
                    token);
                if (message is null)
                {
                    return null;
                }

                onClaimed(message.Id);
                await HandleAsync(message, token);

                return (message.Id, message.Type);
            },
            cancellationToken);

        if (handled is null)
        {
            return false;
        }

        logger.LogInformation(
            "Inbox message {MessageId} of type {MessageType} handled.",
            handled.Value.Id,
            handled.Value.Type);

        return true;
    }

    private async Task HandleAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        using var activity = MessagingActivitySource.Instance
            .StartMessagingActivity(
                ObservabilityConstants.Messaging.Operations.Process,
                ActivityKind.Consumer,
                message.Type,
                message.Id.ToString(),
                message.TraceParent)
            .WithTag(ObservabilityConstants.Messaging.Tags.AttemptCount, message.AttemptCount);

        try
        {
            using var actorScope = holder.Enter(RestoreActor(message));

            await dispatcher.DispatchAsync(message.Type, message.Payload, cancellationToken);
            inbox.MarkProcessed(message);
            await dbContext.SaveChangesAsync(cancellationToken);
            activity.MarkSuccess();
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            activity.MarkException(exception);

            throw;
        }
    }

    private static ActorContext RestoreActor(InboxMessage message) =>
        message.ActorId == SystemActor.Id
            ? SystemActor.Create(message.CompanyId)
            : new ActorContext { UserId = message.ActorId, CompanyId = message.CompanyId };
}
