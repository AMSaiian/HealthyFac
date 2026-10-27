using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Messaging.Persistence;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace HealthyFac.Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Stores each received envelope in the inbox and does nothing else. The inbox processor handles the row later,
/// as the original actor. The handler never falls back to the system actor.
/// <list type="bullet">
/// <item>An invalid message ID (not a GUID, or the empty GUID), an invalid actor header, or an invalid company
/// header throws <see cref="InvalidIntegrationMessageException"/>. No retry can fix these, so Rebus moves the
/// message to the <c>error</c> queue at the first attempt.</item>
/// <item>A payload that the <c>jsonb</c> column cannot store (not JSON text, or a <c>\u0000</c> escape in a string)
/// throws <see cref="InvalidIntegrationMessageException"/> too. The insert would fail on every attempt, so Rebus
/// moves the message to the <c>error</c> queue at the first attempt.</item>
/// <item>The insert runs inside <c>RunInTransactionAsync</c>, because the Upsert SQL alone does not use the
/// retrying execution strategy. Each delivery attempt then waits out a transient database failure.</item>
/// </list>
/// </summary>
/// <param name="inbox">The inbox of the service.</param>
/// <param name="dbContext">The service context, which runs the insert with its retrying execution strategy.</param>
/// <param name="messageContext">The Rebus context of the delivery, which holds the headers.</param>
internal sealed class IntegrationEnvelopeHandler(
    IInbox inbox,
    IInboxDbContext dbContext,
    IMessageContext messageContext
)
    : IHandleMessages<IntegrationEnvelope>
{
    public async Task Handle(IntegrationEnvelope message)
    {
        var headers = messageContext.Headers;
        var rawMessageId = headers.GetValueOrDefault(Headers.MessageId) ?? string.Empty;
        var receivedTraceParent = headers.GetValueOrDefault(MessageHeaders.TraceParent);

        using var activity = MessagingActivitySource.Instance.StartMessagingActivity(
            ObservabilityConstants.Messaging.Operations.Receive,
            ActivityKind.Consumer,
            message.Type,
            rawMessageId,
            receivedTraceParent);

        try
        {
            await StoreAsync(message, headers, rawMessageId, activity?.Id ?? receivedTraceParent);
            activity.MarkSuccess();
        }
        catch (Exception exception) when (!messageContext.GetCancellationToken().IsCancellationRequested)
        {
            activity.MarkException(exception);

            throw;
        }
    }

    private async Task StoreAsync(
        IntegrationEnvelope message,
        Dictionary<string, string> headers,
        string rawMessageId,
        string? traceParent
    )
    {
        var isMessageIdValid = Guid.TryParse(rawMessageId, out var messageId);
        if (!isMessageIdValid || messageId == Guid.Empty)
        {
            throw MessagingErrors.MessageIdInvalid(rawMessageId);
        }

        var isActorValid = Guid.TryParse(headers.GetValueOrDefault(MessageHeaders.ActorId), out var actorId);
        if (!isActorValid || actorId == Guid.Empty)
        {
            throw MessagingErrors.ActorHeaderMissing(rawMessageId);
        }

        var companyId = ReadCompanyId(headers, rawMessageId);

        if (!message.Payload.IsStorableAsJsonb())
        {
            throw MessagingErrors.PayloadNotStorable(rawMessageId, message.Type);
        }

        await dbContext.RunInTransactionAsync(
            token => inbox.TryAddAsync(
                messageId,
                message.Type,
                message.Payload,
                traceParent,
                actorId,
                companyId,
                token),
            messageContext.GetCancellationToken());
    }

    private static Guid? ReadCompanyId(IDictionary<string, string> headers, string rawMessageId)
    {
        if (!headers.TryGetValue(MessageHeaders.CompanyId, out var rawCompanyId))
        {
            return null;
        }

        var isCompanyValid = Guid.TryParse(rawCompanyId, out var companyId);

        return isCompanyValid ? companyId : throw MessagingErrors.CompanyHeaderInvalid(rawMessageId);
    }
}
