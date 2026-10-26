using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

/// <summary>
/// Fails the first inbox insert commands with a transient Npgsql error and lets every later command pass, so a
/// test can prove that the retry strategy waits out a short database outage.
/// </summary>
/// <param name="failureCount">The number of inbox insert commands to fail.</param>
public sealed class InboxInsertFailureInterceptor(int failureCount) : DbCommandInterceptor
{
    private int _injectedCount;

    public int InjectedCount => Volatile.Read(ref _injectedCount);

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        FailInboxInsert(command);

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        FailInboxInsert(command);

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void FailInboxInsert(DbCommand command)
    {
        var isInboxInsert = command.CommandText.Contains(DatabaseOutageTestData.InboxInsertCommand)
            && command.CommandText.Contains(DatabaseOutageTestData.InboxTableName);
        if (isInboxInsert && Interlocked.Increment(ref _injectedCount) <= failureCount)
        {
            throw new NpgsqlException(DatabaseOutageTestData.InjectedFailureText, new TimeoutException());
        }
    }
}
