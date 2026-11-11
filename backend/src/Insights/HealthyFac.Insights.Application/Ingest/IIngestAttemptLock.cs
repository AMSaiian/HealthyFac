using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.Application.Ingest;

/// <summary>
/// Locks the active attempt of a file, so a message handler and the worker never change it at the same time.
/// </summary>
public interface IIngestAttemptLock
{
    /// <summary>
    /// Locks the queued or running attempt of a file of a company and loads it tracked. The lock lasts until the
    /// surrounding transaction ends, so call it inside <c>RunInTransactionAsync</c>.
    /// </summary>
    /// <param name="fileId">The file.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The tracked attempt, or <see langword="null"/> when the file has no active attempt.</returns>
    Task<IngestAttempt?> LockActiveAttemptAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken);

    /// <summary>
    /// Locks the attempt that a file delete must stop: the queued, running, or completed attempt of a file of a
    /// company, and loads it tracked. The unique indexes allow at most one such row for a file. The lock lasts
    /// until the surrounding transaction ends, so call it inside <c>RunInTransactionAsync</c>.
    /// </summary>
    /// <remarks>
    /// The completed state is part of the match on purpose: PostgreSQL checks the <c>WHERE</c> condition again after a
    /// lock wait. A lock on active rows only would return nothing once the worker committed <c>completed</c> while the
    /// delete waited for the row, and the delete would leave the stored document in place.
    /// </remarks>
    /// <param name="fileId">The file.</param>
    /// <param name="companyId">The company of the file.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The tracked attempt, or <see langword="null"/> when no such attempt exists.</returns>
    Task<IngestAttempt?> LockAttemptForFileDeleteAsync(
        Guid fileId,
        Guid companyId,
        CancellationToken cancellationToken
    );
}
