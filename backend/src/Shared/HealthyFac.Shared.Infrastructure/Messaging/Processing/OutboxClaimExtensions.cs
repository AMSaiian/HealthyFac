using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// Claims the next pending outbox row with a row lock. EF Core cannot express <c>FOR UPDATE SKIP LOCKED</c>, so
/// the claim goes through linq2db on the connection and the transaction of the EF Core context. linq2db returns
/// only the row ID, because it does not read the <c>xmin</c> shadow property that EF Core uses as the concurrency
/// token. EF Core then loads the locked row, so every write stays in EF Core.
/// </summary>
internal static class OutboxClaimExtensions
{
    extension(IOutboxDbContext dbContext)
    {
        /// <summary>
        /// Locks the next due row and loads it tracked on the EF Core context, so a later
        /// <c>SaveChangesAsync</c> writes its change through EF Core with the concurrency check.
        /// </summary>
        /// <param name="now">The UTC time that decides which rows are due.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>The tracked row, or <see langword="null"/> when no due row is free.</returns>
        public async Task<OutboxMessage?> ClaimNextOutboxMessageAsync(
            DateTime now,
            CancellationToken cancellationToken
        )
        {
            var claimedIds = await dbContext.BuildOutboxClaimQuery(now).ToListAsyncLinqToDB(cancellationToken);
            if (claimedIds.Count == 0)
            {
                return null;
            }

            return await dbContext.OutboxMessages.SingleAsync(
                message => message.Id == claimedIds[0],
                cancellationToken);
        }

        /// <summary>
        /// Builds the claim query. The hint locks the selected row and skips rows that another transaction
        /// locked, so two processors never claim the same row. The lock lasts until the surrounding transaction
        /// ends, so run the claim inside <c>RunInTransactionAsync</c>.
        /// </summary>
        /// <param name="now">The UTC time that decides which rows are due.</param>
        /// <returns>A query for the ID of at most one due row, oldest first.</returns>
        public IQueryable<Guid> BuildOutboxClaimQuery(DateTime now) =>
            dbContext.OutboxMessages
                .Where(message => message.ProcessedOn == null && message.NextAttemptOn <= now)
                .OrderBy(message => message.OccurredOn)
                .Select(message => message.Id)
                .Take(1)
                .ToLinqToDB()
                .AsPostgreSQL()
                .SubQueryTableHint(PostgreSQLHints.ForUpdate, PostgreSQLHints.SkipLocked);
    }
}
