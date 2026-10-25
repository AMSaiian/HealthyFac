namespace HealthyFac.Shared.Application.Persistence;

/// <summary>
/// Declares the explicit transaction of a service context. The Application interface of a service (e.g.,
/// <c>ICoreDbContext</c>) inherits it, so use cases can run several saves in one transaction. Each context implements
/// both methods with the same code as <c>CoreDbContext</c>.
/// </summary>
public interface ITransactionalDbContext
{
    /// <summary>
    /// Runs <paramref name="operation"/> in one transaction and commits. Use it only when one <c>SaveChangesAsync</c>
    /// call is not enough (e.g., lock a row, handle it, and mark it processed as one unit). The operation saves its
    /// changes with <c>SaveChangesAsync</c>, as often as it needs. It must never commit or roll back. To abort, it
    /// throws, and the transaction rolls back. On a transient failure, the retry strategy runs the whole unit again, so
    /// the operation must be safe to repeat. Throws <see cref="InvalidOperationException"/> on unsaved changes before
    /// the call or after the operation, or on a transaction that the operation ended.
    /// </summary>
    /// <remarks>
    /// Each attempt clears the change tracker first, so the changes of a failed attempt are not saved again. Read and
    /// change every entity inside the operation: an entity loaded before the call is no longer tracked.
    /// </remarks>
    /// <param name="operation">The work to run in the transaction.</param>
    /// <param name="cancellationToken">Cancels the operation and rolls back the transaction.</param>
    /// <returns>A task that completes when the transaction commits.</returns>
    /// <exception cref="InvalidOperationException">The context has unsaved changes before the call or after the
    /// operation, or the operation committed or rolled back the transaction.</exception>
    Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    );

    /// <summary>
    /// Runs <paramref name="operation"/> in one transaction, commits, and returns the result of the operation. Use it
    /// only when one <c>SaveChangesAsync</c> call is not enough. The operation saves its changes with
    /// <c>SaveChangesAsync</c>, as often as it needs. It must never commit or roll back. To abort, it throws, and the
    /// transaction rolls back. On a transient failure, the retry strategy runs the whole unit again, so the operation
    /// must be safe to repeat. Throws <see cref="InvalidOperationException"/> on unsaved changes before the call or
    /// after the operation, or on a transaction that the operation ended.
    /// </summary>
    /// <remarks>
    /// Each attempt clears the change tracker first, so the changes of a failed attempt are not saved again. Read and
    /// change every entity inside the operation: an entity loaded before the call is no longer tracked.
    /// </remarks>
    /// <typeparam name="TResult">The type of the operation result.</typeparam>
    /// <param name="operation">The work to run in the transaction.</param>
    /// <param name="cancellationToken">Cancels the operation and rolls back the transaction.</param>
    /// <returns>The result of the attempt that committed.</returns>
    /// <exception cref="InvalidOperationException">The context has unsaved changes before the call or after the
    /// operation, or the operation committed or rolled back the transaction.</exception>
    Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    );
}
