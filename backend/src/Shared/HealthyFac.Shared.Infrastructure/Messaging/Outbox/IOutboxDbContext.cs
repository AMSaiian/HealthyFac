using HealthyFac.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Marks a context that maps the outbox table, so <see cref="Outbox"/> can add rows to its business transaction.
/// It is transactional, so the outbox processor claims, publishes, and marks a row in one transaction.
/// </summary>
public interface IOutboxDbContext : ITransactionalDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>
    /// Saves the tracked changes. The outbox processor calls it inside the operation of
    /// <see cref="ITransactionalDbContext.RunInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>,
    /// because the transaction commits only saved changes.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
