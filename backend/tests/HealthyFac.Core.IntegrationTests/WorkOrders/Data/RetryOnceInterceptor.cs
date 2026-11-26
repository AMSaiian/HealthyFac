using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HealthyFac.Core.IntegrationTests.WorkOrders.Data;

/// <summary>
/// Fails the next write command with the serialization failure <c>40001</c> once after <see cref="Arm"/>, so the
/// execution strategy repeats the unit of work and a test sees what the repeat does.
/// </summary>
public sealed class RetryOnceInterceptor : DbCommandInterceptor
{
    private const string SerializationFailure = "40001";
    private const string FailureText = "simulated serialization failure";
    private const string UpdateCommand = "UPDATE";
    private const string InsertCommand = "INSERT";

    private int _armed;
    private int _injectedCount;

    public int InjectedCount => Volatile.Read(ref _injectedCount);

    public void Arm() => Interlocked.Exchange(ref _armed, value: 1);

    public void Disarm()
    {
        Interlocked.Exchange(ref _armed, value: 0);
        Interlocked.Exchange(ref _injectedCount, value: 0);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        FailOnce(command);

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        FailOnce(command);

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void FailOnce(DbCommand command)
    {
        var isWrite = command.CommandText.Contains(UpdateCommand) || command.CommandText.Contains(InsertCommand);

        if (isWrite && Interlocked.CompareExchange(ref _armed, value: 0, comparand: 1) == 1)
        {
            Interlocked.Increment(ref _injectedCount);

            throw new PostgresException(
                FailureText, severity: "ERROR", invariantSeverity: "ERROR", SerializationFailure);
        }
    }
}
