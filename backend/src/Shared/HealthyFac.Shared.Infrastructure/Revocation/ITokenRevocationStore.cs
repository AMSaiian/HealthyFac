namespace HealthyFac.Shared.Infrastructure.Revocation;

/// <summary>
/// Stores the time when the sessions of a user ended. The gateway rejects a token that was issued before that time.
/// The Auth service writes an entry when it deactivates an employee, and the gateway reads it.
/// </summary>
public interface ITokenRevocationStore
{
    /// <summary>
    /// Marks the tokens of a user that were issued before <paramref name="revokedAt"/> as revoked.
    /// </summary>
    /// <param name="userId">The Keycloak user ID (the <c>sub</c> claim).</param>
    /// <param name="revokedAt">The time when the tokens became invalid.</param>
    /// <param name="timeToLive">How long the entry lives. It must exceed the lifetime of an access token.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting for Redis.</param>
    /// <returns>A task that completes when Redis stores the entry.</returns>
    Task RevokeAsync(Guid userId, DateTimeOffset revokedAt, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the revocation time of a user.
    /// </summary>
    /// <param name="userId">The Keycloak user ID (the <c>sub</c> claim).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting for Redis.</param>
    /// <returns>The revocation time, or <see langword="null"/> when the user has no entry.</returns>
    Task<DateTimeOffset?> GetRevokedAtAsync(Guid userId, CancellationToken cancellationToken);
}
