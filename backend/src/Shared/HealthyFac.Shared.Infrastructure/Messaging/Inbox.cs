using HealthyFac.Shared.Application.Time;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Inserts inbox rows through the scoped service context. <see cref="MessagingExtensions"/> registers the context
/// interfaces against one instance, so the insert uses the same connection and transaction as the service.
/// </summary>
/// <param name="dbContext">The scoped service context.</param>
/// <param name="dateProvider">The clock for the time of the delivery.</param>
internal sealed class Inbox(IInboxDbContext dbContext, IDateProvider dateProvider) : IInbox
{
    public async Task<bool> TryAddAsync(
        Guid messageId,
        string type,
        string payload,
        string? traceParent,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var insertedCount = await dbContext.InboxMessages
            .Upsert(new InboxMessage(messageId, type, payload, dateProvider.GetUtc(), traceParent))
            .On(message => message.Id)
            .NoUpdate()
            .RunAsync(cancellationToken);

        return insertedCount > 0;
    }

    public void MarkProcessed(InboxMessage message)
    {
        if (!dbContext.InboxMessages.Local.Contains(message))
        {
            throw MessagingErrors.InboxMessageNotTracked(message.Id);
        }

        message.MarkProcessed(dateProvider.GetUtc());
    }
}
