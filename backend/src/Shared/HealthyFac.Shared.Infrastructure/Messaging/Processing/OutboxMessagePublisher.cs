using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Messages;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// The result of one publish step of the outbox.
/// </summary>
internal enum ClaimResult
{
    None,
    Published,
    Failed,
}

/// <summary>
/// Publishes one pending outbox row in its own transaction. The class is scoped, so it shares the service
/// context of the scope that the outbox processor opens for the row. The publisher locks the row, publishes it to
/// the topic that is named after the message type, and marks the row. A publish failure records the attempt and
/// leaves the row pending. A crash before the commit leaves the row pending too, so the broker can receive a
/// message twice. The row ID is the message ID, and the inbox of the receiver drops the duplicate.
/// </summary>
/// <param name="dbContext">The service context that holds the outbox set.</param>
/// <param name="bus">The Rebus bus of the service.</param>
/// <param name="timeProvider">The clock for the due time of the rows and the publish duration.</param>
/// <param name="metrics">Counts the published and the retried messages.</param>
/// <param name="logger">The logger of the publisher.</param>
internal sealed class OutboxMessagePublisher(
    IOutboxDbContext dbContext,
    IBus bus,
    TimeProvider timeProvider,
    MessagingMetrics metrics,
    ILogger<OutboxMessagePublisher> logger
)
{
    private readonly IOutboxDbContext _dbContext = dbContext;
    private readonly IBus _bus = bus;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly MessagingMetrics _metrics = metrics;
    private readonly ILogger<OutboxMessagePublisher> _logger = logger;

    /// <summary>
    /// Claims the next due row and publishes it.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>
    /// <see cref="ClaimResult.None"/> when no row is due, <see cref="ClaimResult.Published"/> when the broker
    /// accepted the row, and <see cref="ClaimResult.Failed"/> when the publish failed and the row is scheduled
    /// for a retry.
    /// </returns>
    public async Task<ClaimResult> PublishNextAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.RunInTransactionAsync(
            async token =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var message = await _dbContext.ClaimNextOutboxMessageAsync(now, token);
                if (message is null)
                {
                    return ClaimResult.None;
                }

                var isPublished = await TryPublishAsync(message, now, token);
                await _dbContext.SaveChangesAsync(token);

                return isPublished ? ClaimResult.Published : ClaimResult.Failed;
            },
            cancellationToken);
    }

    /// <summary>
    /// Publishes the claimed row and records the result on the row and in the metrics. A cancellation records
    /// nothing, because it is not a failure.
    /// </summary>
    /// <param name="message">The claimed row.</param>
    /// <param name="now">The UTC time of the claim.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="true"/> when the broker accepted the message.</returns>
    private async Task<bool> TryPublishAsync(OutboxMessage message, DateTime now, CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetTimestamp();
        using var activity = MessagingActivitySource.Instance.StartMessagingActivity(
            ObservabilityConstants.Messaging.Operations.Send,
            ActivityKind.Producer,
            message.Type,
            message.Id.ToString(),
            message.TraceParent);

        try
        {
            await _bus.Advanced.Topics.Publish(
                message.Type,
                new IntegrationEnvelope { Type = message.Type, Payload = message.Payload },
                CreateHeaders(message, activity));
            message.MarkPublished(now);
            activity.MarkSuccess();
            _metrics.RecordPublished(message.Type);
            RecordPublish(message, ObservabilityConstants.Outcomes.Success, startedAt);

            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            activity.MarkException(exception);
            _logger.LogWarning(
                exception,
                "Publishing outbox message {MessageId} of type {MessageType} failed.",
                message.Id,
                message.Type);
            message.ScheduleRetry(now, exception.Message);
            _metrics.RecordRetried(ObservabilityConstants.Messaging.Processors.Outbox, message.Type);
            RecordPublish(message, ObservabilityConstants.Outcomes.Failure, startedAt);

            return false;
        }
    }

    private void RecordPublish(OutboxMessage message, string outcome, long startedAt) =>
        _metrics.RecordHandle(
            ObservabilityConstants.Messaging.Processors.Outbox,
            message.Type,
            outcome,
            _timeProvider.GetElapsedTime(startedAt));

    private static Dictionary<string, string> CreateHeaders(OutboxMessage message, Activity? sendActivity)
    {
        var headers = new Dictionary<string, string>
        {
            [Headers.MessageId] = message.Id.ToString(),
            [ActorHeaderNames.ActorId] = message.ActorId.ToString(),
        };

        if (message.CompanyId is not null)
        {
            headers[ActorHeaderNames.CompanyId] = message.CompanyId.Value.ToString();
        }

        var traceParent = sendActivity?.Id ?? message.TraceParent;
        if (traceParent is not null)
        {
            headers[MessageHeaders.TraceParent] = traceParent;
        }

        return headers;
    }
}
