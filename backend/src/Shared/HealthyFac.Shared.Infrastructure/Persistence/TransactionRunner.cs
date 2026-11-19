using System.Data;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Runs an operation in a transaction for every service context. It is the only place that begins a transaction
/// (the architecture tests check it), so the retry and the join rules exist once.
/// </summary>
/// <remarks>
/// Without an open transaction, each attempt runs inside the execution strategy, because the retry strategy rejects a
/// transaction that begins outside a strategy block. Inside an open transaction, the operation joins it and runs
/// once: the outermost strategy block retries the whole unit, and the outer call commits.
/// </remarks>
public static class TransactionRunner
{
    /// <summary>
    /// Runs <paramref name="operation"/> in a transaction and returns its result. Without an open transaction, it
    /// begins one, retries the whole unit on a transient failure, and commits. Inside an open transaction, it joins
    /// it: one run, no retry, no tracker clear, no commit.
    /// </summary>
    /// <typeparam name="TResult">The type of the operation result.</typeparam>
    /// <param name="dbContext">The context that runs the operation.</param>
    /// <param name="operation">The work to run. It saves its changes and never ends the transaction.</param>
    /// <param name="isolationLevel">The isolation level of a new transaction, or of the open one to join.</param>
    /// <param name="cancellationToken">Cancels the operation and rolls back a transaction that this call began.</param>
    /// <returns>The result of the attempt that committed, or of the joined run.</returns>
    /// <exception cref="InvalidOperationException">The context has unsaved changes before the call or after the
    /// operation, the operation ended the transaction, or the isolation level differs from the level of the open
    /// transaction.</exception>
    public static async Task<TResult> RunAsync<TResult>(
        DbContext dbContext,
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken
    )
    {
        if (dbContext.ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesBeforeTransaction();
        }

        var openTransaction = dbContext.Database.CurrentTransaction;
        if (openTransaction is not null)
        {
            return await JoinAsync(dbContext, openTransaction, operation, isolationLevel, cancellationToken);
        }

        return await dbContext.Database
            .CreateExecutionStrategy()
            .ExecuteAsync(
                token => RunAttemptAsync(dbContext, operation, isolationLevel, token),
                cancellationToken);
    }

    /// <summary>
    /// Saves the changes of <paramref name="dbContext"/>. A save with a soft delete cascade runs in one transaction
    /// inside the execution strategy (the pattern of the EF Core connection resiliency documentation), so the
    /// interceptor statements and the save commit together. A retry repeats both, because the deleted principals stay
    /// detectable (<c>Modified</c> with the original <c>DeletedAt</c> null). Inside an open transaction, or without a
    /// cascade, the save runs as EF Core runs it.
    /// </summary>
    /// <param name="dbContext">The context to save.</param>
    /// <param name="save">The base <c>SaveChangesAsync</c> of the context.</param>
    /// <param name="isAcceptingAllChanges">Whether to accept the changes in the tracker after the save.</param>
    /// <param name="cancellationToken">Cancels the save and rolls back a transaction that this call began.</param>
    /// <returns>The number of state entries that the save wrote.</returns>
    public static async Task<int> SaveAsync(
        DbContext dbContext,
        Func<bool, CancellationToken, Task<int>> save,
        bool isAcceptingAllChanges,
        CancellationToken cancellationToken
    )
    {
        if (dbContext.Database.CurrentTransaction is not null || !SoftDeleteCascade.HasCascadeWork(dbContext))
        {
            return await save(isAcceptingAllChanges, cancellationToken);
        }

        var count = await dbContext.Database
            .CreateExecutionStrategy()
            .ExecuteAsync(
                async token =>
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.ReadCommitted, token);
                    var attemptCount = await save(arg1: false, token);
                    await transaction.CommitAsync(token);

                    return attemptCount;
                },
                cancellationToken);
        if (isAcceptingAllChanges)
        {
            dbContext.ChangeTracker.AcceptAllChanges();
        }

        return count;
    }

    private static async Task<TResult> JoinAsync<TResult>(
        DbContext dbContext,
        IDbContextTransaction openTransaction,
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken
    )
    {
        var openLevel = openTransaction.GetDbTransaction().IsolationLevel;
        if (openLevel != isolationLevel)
        {
            throw PersistenceErrors.IsolationLevelDiffersFromOpenTransaction(openLevel, isolationLevel);
        }

        var result = await operation(cancellationToken);
        CheckAfterOperation(dbContext, openTransaction);

        return result;
    }

    private static async Task<TResult> RunAttemptAsync<TResult>(
        DbContext dbContext,
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken
    )
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        var result = await operation(cancellationToken);
        CheckAfterOperation(dbContext, transaction);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private static void CheckAfterOperation(
        DbContext dbContext,
        IDbContextTransaction transaction
    )
    {
        if (dbContext.Database.CurrentTransaction != transaction)
        {
            throw PersistenceErrors.TransactionEndedByOperation();
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesAfterOperation();
        }
    }
}
