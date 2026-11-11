using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

/// <summary>
/// Counts the database transactions that the context starts. A test reads the count before and after a race: a call
/// that the execution strategy repeats starts one transaction for each attempt, so the count shows the repeat.
/// </summary>
public sealed class TransactionCounter : DbTransactionInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public override ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Interlocked.Increment(ref _count);

        return ValueTask.FromResult(result);
    }
}
