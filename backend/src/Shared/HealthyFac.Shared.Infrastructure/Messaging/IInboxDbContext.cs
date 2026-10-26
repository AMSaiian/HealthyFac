using HealthyFac.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Marks a context that maps the inbox table, so a consumer can store a received message before it handles it.
/// It is transactional, so the inbox processor claims, handles, and marks a row in one transaction.
/// </summary>
public interface IInboxDbContext : ITransactionalDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }

    /// <summary>
    /// Saves the tracked changes. The inbox processor calls it inside the operation of
    /// <see cref="ITransactionalDbContext.RunInTransactionAsync(Func{CancellationToken, Task}, CancellationToken)"/>,
    /// because the transaction commits only saved changes.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
