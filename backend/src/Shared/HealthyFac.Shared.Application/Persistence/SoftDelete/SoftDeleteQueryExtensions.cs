using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Application.Persistence.SoftDelete;

/// <summary>
/// Soft-deletes the rows of a query in one statement, without loading them. Use it for a collection of a root that
/// has no bound (e.g., a history) and that the use case does not load.
/// </summary>
public static class SoftDeleteQueryExtensions
{
    extension<T>(IQueryable<T> query)
        where T : class, ISoftDeletable, IAuditable
    {
        /// <summary>
        /// Soft-deletes the rows that the query returns and that are not deleted yet. <c>ExecuteUpdate</c> skips the
        /// soft delete and audit interceptors, so the method sets <c>DeletedAt</c>, <c>DeletedBy</c>,
        /// <c>UpdatedAt</c>, and <c>UpdatedBy</c> itself. A row that is already deleted keeps its delete time.
        /// </summary>
        /// <param name="dbContext">The context that holds the open transaction.</param>
        /// <param name="timeProvider">Provides the time of the delete.</param>
        /// <param name="currentUserProvider">Provides the actor of the delete.</param>
        /// <param name="cancellationToken">Cancels the statement.</param>
        /// <returns>The number of rows that the statement soft-deleted.</returns>
        /// <exception cref="InvalidOperationException">No transaction is open on <paramref name="dbContext"/>.
        /// </exception>
        public async Task<int> SoftDeleteAsync(
            ITransactionalDbContext dbContext,
            TimeProvider timeProvider,
            ICurrentUserProvider currentUserProvider,
            CancellationToken cancellationToken
        )
        {
            if (dbContext.Database.CurrentTransaction is null)
            {
                throw SoftDeleteErrors.NoOpenTransaction();
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var userId = currentUserProvider.TryGetUserId();

            return await query
                .Where(row => row.DeletedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(row => EF.Property<DateTime?>(row, nameof(ISoftDeletable.DeletedAt)), now)
                        .SetProperty(row => EF.Property<Guid?>(row, nameof(ISoftDeletable.DeletedBy)), userId)
                        .SetProperty(row => EF.Property<DateTime?>(row, nameof(IAuditable.UpdatedAt)), now)
                        .SetProperty(row => EF.Property<Guid?>(row, nameof(IAuditable.UpdatedBy)), userId),
                    cancellationToken);
        }
    }
}
