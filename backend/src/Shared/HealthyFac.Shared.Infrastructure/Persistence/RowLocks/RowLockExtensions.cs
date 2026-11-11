using System.Linq.Expressions;
using HealthyFac.Shared.Domain.Entities;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HealthyFac.Shared.Infrastructure.Persistence.RowLocks;

/// <summary>
/// Locks one row with <c>FOR UPDATE</c> and loads it tracked. EF Core cannot express the lock hint, so linq2db selects
/// the ID on the connection and the transaction of the EF Core context, and EF Core loads the locked row. Every write
/// stays in EF Core (see the linq2db rule in <c>backend-infrastructure.md</c>).
/// </summary>
public static class RowLockExtensions
{
    extension<TEntity>(DbSet<TEntity> set)
        where TEntity : class, IEntity
    {
        /// <summary>
        /// Locks the first row that matches <paramref name="predicate"/> and loads it tracked. The lock lasts until the
        /// surrounding transaction ends, so run the call inside <c>RunInTransactionAsync</c>. A parallel caller waits
        /// for the lock and then sees the committed row.
        /// </summary>
        /// <remarks>
        /// When the context already tracks the locked row, EF Core would return the tracked instance with old values.
        /// The method then reloads the entry from the database, so the caller always sees the committed row. The reload
        /// drops unsaved changes of that entry.
        /// </remarks>
        /// <param name="predicate">Selects the candidate rows. The query filters of the entity apply.</param>
        /// <param name="cancellationToken">The token that cancels the call.</param>
        /// <returns>The tracked row, or <see langword="null"/> when no row matches.</returns>
        public Task<TEntity?> LockFirstAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken
        ) =>
            set.LockAsync(predicate, entity => entity.Id, isSkippingLocked: false, cancellationToken);

        /// <summary>
        /// Claims the first row in the order of <paramref name="orderBy"/> that matches <paramref name="predicate"/>,
        /// skips a row that another transaction locked, and loads the claimed row tracked. Two workers then never take
        /// the same row, and neither waits for the other. The lock lasts until the surrounding transaction ends, so run
        /// the call inside <c>RunInTransactionAsync</c>.
        /// </summary>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="predicate">Selects the candidate rows. The query filters of the entity apply.</param>
        /// <param name="orderBy">The sort key. The lowest value is claimed first.</param>
        /// <param name="cancellationToken">The token that cancels the call.</param>
        /// <returns>The tracked row, or <see langword="null"/> when no free row matches.</returns>
        public Task<TEntity?> ClaimFirstAsync<TKey>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TKey>> orderBy,
            CancellationToken cancellationToken
        ) =>
            set.LockAsync(predicate, orderBy, isSkippingLocked: true, cancellationToken);

        private async Task<TEntity?> LockAsync<TKey>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TKey>> orderBy,
            bool isSkippingLocked,
            CancellationToken cancellationToken
        )
        {
            var query = set
                .Where(predicate)
                .OrderBy(orderBy)
                .Select(entity => entity.Id)
                .Take(count: 1)
                .ToLinqToDB()
                .AsPostgreSQL();
            var lockedIds = await (isSkippingLocked
                    ? query.SubQueryTableHint(PostgreSQLHints.ForUpdate, PostgreSQLHints.SkipLocked)
                    : query.SubQueryTableHint(PostgreSQLHints.ForUpdate))
                .ToListAsyncLinqToDB(cancellationToken);
            if (lockedIds.Count == 0)
            {
                return null;
            }

            var lockedId = lockedIds[index: 0];
            var context = set.GetService<ICurrentDbContext>().Context;
            var isTracked = context.ChangeTracker.Entries<TEntity>().Any(entry => entry.Entity.Id == lockedId);
            var locked = await set.SingleAsync(entity => entity.Id == lockedId, cancellationToken);
            if (isTracked)
            {
                await context.Entry(locked).ReloadAsync(cancellationToken);
            }

            return locked;
        }
    }
}
