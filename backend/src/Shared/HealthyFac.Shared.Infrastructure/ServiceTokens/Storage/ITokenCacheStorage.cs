namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Keeps the token entries and the fetch locks of the <see cref="TokenStore"/>, so the instances of a service share
/// them.
/// </summary>
/// <remarks>
/// Every method throws <see cref="TokenStorageException"/> when the storage is down, too slow, or skipped by a
/// circuit breaker. The store then fetches the token itself, so a storage failure never fails a call. A cancelled
/// call throws <see cref="OperationCanceledException"/>, and that is not a storage failure. The entries are not
/// encrypted, so the storage must stay on a private network with authentication.
/// </remarks>
public interface ITokenCacheStorage
{
    /// <summary>
    /// Reads an entry.
    /// </summary>
    /// <param name="key">The key of the entry (see <c>TokenCacheKeys</c>).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The stored value, or <see langword="null"/> when the key has no entry.</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Writes an entry that the storage removes when its time to live ends.
    /// </summary>
    /// <param name="key">The key of the entry (see <c>TokenCacheKeys</c>).</param>
    /// <param name="value">The value to store.</param>
    /// <param name="timeToLive">The time after which the storage removes the entry.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the storage has the entry.</returns>
    Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Takes a lock when no one holds it. The check and the write are one atomic step, and the lock ends by itself
    /// after its time to live, so a holder that crashed never blocks the others for ever.
    /// </summary>
    /// <param name="key">The key of the lock.</param>
    /// <param name="owner">The value that names the holder. Only this value releases the lock.</param>
    /// <param name="timeToLive">The time after which the lock ends by itself.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="true"/> when the caller holds the lock; <see langword="false"/> when another holder
    /// has it.</returns>
    Task<bool> TryAcquireLockAsync(
        string key,
        string owner,
        TimeSpan timeToLive,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Releases a lock only when <paramref name="owner"/> still holds it. After the time to live ended, another
    /// holder can own the key, and this call then leaves that lock in place.
    /// </summary>
    /// <param name="key">The key of the lock.</param>
    /// <param name="owner">The value that the holder gave to <see cref="TryAcquireLockAsync"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the storage has handled the release.</returns>
    Task ReleaseLockAsync(string key, string owner, CancellationToken cancellationToken);
}
