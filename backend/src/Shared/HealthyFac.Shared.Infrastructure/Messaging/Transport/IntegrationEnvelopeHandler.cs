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

        await dbContext.RunInTransactionAsync(
            token => inbox.TryAddAsync(
                messageId,
                message.Type,
                message.Payload,
                headers.GetValueOrDefault(MessageHeaders.TraceParent),
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
