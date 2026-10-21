using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Marks a context that maps the outbox table, so <see cref="Outbox"/> can add rows to its business transaction.
/// </summary>
public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
