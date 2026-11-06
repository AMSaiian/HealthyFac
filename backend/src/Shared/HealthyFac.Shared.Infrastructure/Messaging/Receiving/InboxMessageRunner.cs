using System.Diagnostics;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Messaging.Receiving;

/// <summary>
/// Handles one pending inbox row in its own transaction. The class is scoped, so the handler of the message
/// shares the service context of the scope that the inbox processor opens for the row. The runner locks the row
/// as the inbox processor actor of the service, enters the restored actor of the row, calls the typed handler, and
/// marks the row. The handler changes and the processed state commit together. A failure rolls the whole unit back.
/// </summary>
/// <param name="dbContext">The service context that holds the inbox set.</param>
/// <param name="inbox">The inbox that marks a row as processed.</param>
/// <param name="dispatcher">Calls the typed handler of the message.</param>
/// <param name="holder">The holder of the actor of the scope.</param>
/// <param name="timeProvider">The clock for the due time of the rows and the handling duration.</param>
/// <param name="serviceOptions">The name of the running service, for the actor of the claim step.</param>
/// <param name="metrics">Counts the handled messages and records the handling duration.</param>
/// <param name="logger">The logger of the runner.</param>
internal sealed class InboxMessageRunner(
    IInboxDbContext dbContext,
    IInbox inbox,
    IntegrationMessageDispatcher dispatcher,
    ActorContextHolder holder,
    TimeProvider timeProvider,
    IOptions<ServiceOptions> serviceOptions,
    MessagingMetrics metrics,
    ILogger<InboxMessageRunner> logger
)
{
    private readonly IInboxDbContext _dbContext = dbContext;
    private readonly IInbox _inbox = inbox;
    private readonly IntegrationMessageDispatcher _dispatcher = dispatcher;
    private readonly ActorContextHolder _holder = holder;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<ServiceOptions> _serviceOptions = serviceOptions;
    private readonly MessagingMetrics _metrics = metrics;
    private readonly ILogger<InboxMessageRunner> _logger = logger;

    /// <summary>
    /// Claims the next due row and handles it. After the transaction commits, the runner logs the handled message
    /// at <c>Information</c> and counts it. A retried or rolled-back attempt never logs or counts it.
    /// </summary>
    /// <param name="onClaimed">Receives the ID of the claimed row, so the processor can schedule a retry after a
    /// failure.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="true"/> when a row was handled, and <see langword="false"/> when no row is due.
    /// </returns>
    public async Task<bool> RunNextAsync(Action<Guid> onClaimed, CancellationToken cancellationToken)
    {
        var handled = await _dbContext.RunInTransactionAsync<(Guid Id, string Type)?>(
            async token =>
            {
                var message = await ClaimNextAsync(token);
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

        _logger.LogInformation(
            message: "Inbox message {MessageId} of type {MessageType} handled.",
            handled.Value.Id,
            handled.Value.Type);
        _metrics.RecordHandled(handled.Value.Type);

        return true;
    }

    private async Task<InboxMessage?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        using var actorScope = _holder.Enter(
            ServiceActor.InboxProcessor.For(_serviceOptions.Value.ToServiceName()));

        return await _dbContext.ClaimNextInboxMessageAsync(_timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    /// <summary>
    /// Calls the handler of the row as its restored actor and marks the row processed. The handling duration is
    /// recorded with its outcome; a cancellation records nothing, because it is not a failure.
    /// </summary>
    /// <param name="message">The claimed row.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the row is handled and saved.</returns>
    private async Task HandleAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetTimestamp();
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
            using var actorScope = _holder.Enter(KnownActors.Restore(message.ActorId, message.CompanyId));

            await _dispatcher.DispatchAsync(message.Type, message.Payload, cancellationToken);
            _inbox.MarkProcessed(message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            activity.MarkSuccess();
            RecordHandle(message, ObservabilityConstants.Outcomes.Success, startedAt);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            activity.MarkException(exception);
            RecordHandle(message, ObservabilityConstants.Outcomes.Failure, startedAt);

            throw;
        }
    }

    private void RecordHandle(InboxMessage message, string outcome, long startedAt) =>
        _metrics.RecordHandle(
            ObservabilityConstants.Messaging.Processors.Inbox,
            message.Type,
            outcome,
            _timeProvider.GetElapsedTime(startedAt));
}
