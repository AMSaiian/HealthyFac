using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Messaging.Processing;

/// <summary>
/// Claims the next pending inbox row with a row lock. It follows the shape of <see cref="OutboxClaimExtensions"/>:
/// linq2db locks the row on the connection and the transaction of the EF Core context, and EF Core loads the
/// locked row, so every write stays in EF Core.
/// </summary>
internal static class InboxClaimExtensions
{
    extension(IInboxDbContext dbContext)
    {
        /// <summary>
        /// Locks the next due row and loads it tracked on the EF Core context, so the processed state saves with
        /// the business change of the handler.
        /// </summary>
        /// <param name="now">The UTC time that decides which rows are due.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>The tracked row, or <see langword="null"/> when no due row is free.</returns>
        public async Task<InboxMessage?> ClaimNextInboxMessageAsync(
            DateTime now,
            CancellationToken cancellationToken
        )
        {
            var claimedIds = await dbContext.BuildInboxClaimQuery(now).ToListAsyncLinqToDB(cancellationToken);
            if (claimedIds.Count == 0)
            {
                return null;
            }

            return await dbContext.InboxMessages.SingleAsync(
                message => message.Id == claimedIds[0],
                cancellationToken);
        }

        /// <summary>
        /// Builds the claim query. The hint locks the selected row and skips rows that another transaction
        /// locked. The lock lasts until the surrounding transaction ends, so run the claim inside
        /// <c>RunInTransactionAsync</c>.
        /// </summary>
        /// <param name="now">The UTC time that decides which rows are due.</param>
        /// <returns>A query for the ID of at most one due row, oldest first.</returns>
        public IQueryable<Guid> BuildInboxClaimQuery(DateTime now) =>
            dbContext.InboxMessages
                .Where(message =>
                    message.ProcessedOn == null
                    && message.FailedOn == null
                    && message.NextAttemptOn <= now)
                .OrderBy(message => message.ReceivedOn)
                .Select(message => message.Id)
                .Take(1)
                .ToLinqToDB()
                .AsPostgreSQL()
                .SubQueryTableHint(PostgreSQLHints.ForUpdate, PostgreSQLHints.SkipLocked);
    }
}
