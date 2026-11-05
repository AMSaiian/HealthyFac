using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging.Receiving;

/// <summary>
/// Inserts inbox rows through the scoped service context. <see cref="MessagingExtensions"/> registers the context
/// interfaces against one instance, so the insert uses the same connection and transaction as the service.
/// </summary>
/// <param name="dbContext">The scoped service context.</param>
/// <param name="timeProvider">The clock for the time of the delivery.</param>
internal sealed class Inbox(IInboxDbContext dbContext, TimeProvider timeProvider) : IInbox
{
    private readonly IInboxDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<bool> TryAddAsync(
        Guid messageId,
        string type,
        string payload,
        string? traceParent,
        Guid actorId,
        Guid? companyId,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var insertedCount = await _dbContext.InboxMessages
            .Upsert(
                new InboxMessage(
                    messageId,
                    type,
                    payload,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    traceParent,
                    actorId,
                    companyId))
            .On(message => message.Id)
            .NoUpdate()
            .RunAsync(cancellationToken);

        return insertedCount > 0;
    }

    public void MarkProcessed(InboxMessage message)
    {
        if (!_dbContext.InboxMessages.Local.Contains(message))
        {
            throw MessagingErrors.InboxMessageNotTracked(message.Id);
        }

        message.MarkProcessed(_timeProvider.GetUtcNow().UtcDateTime);
    }
}
