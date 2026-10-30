namespace HealthyFac.Auth.Application.Identities;

/// <summary>
/// Ends the sessions of a user at the gateway. The gateway rejects an access token of the user that was issued before
/// the revocation time, so a deactivated employee loses access before the token expires.
/// </summary>
public interface ISessionRevoker
{
    /// <summary>
    /// Marks every access token of <paramref name="userId"/> that was issued before <paramref name="revokedAt"/> as
    /// revoked. A retry is safe, because a later revocation time only rejects more tokens.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <param name="revokedAt">The time when the sessions ended.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting.</param>
    /// <returns>A task that completes when the entry is stored.</returns>
    Task RevokeAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
}
