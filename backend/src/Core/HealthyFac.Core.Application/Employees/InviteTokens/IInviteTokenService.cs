namespace HealthyFac.Core.Application.Employees.InviteTokens;

/// <summary>
/// Creates and checks invite tokens. A token has the format <c>{inviteId:N}.{keyId}.{signature}</c>. The signature is
/// an HMAC-SHA256 over the invite ID bytes and the invite salt. The token is never stored: a renewed salt makes every
/// older token invalid.
/// </summary>
public interface IInviteTokenService
{
    /// <summary>
    /// Creates a random salt for a new or renewed invite.
    /// </summary>
    /// <returns>A new cryptographically random salt.</returns>
    byte[] CreateSalt();

    /// <summary>
    /// Calculates the moment when an invite issued at the given time expires.
    /// </summary>
    /// <param name="issuedAt">The UTC moment of issue.</param>
    /// <returns>The UTC moment of expiry.</returns>
    DateTime GetExpiresAt(DateTime issuedAt);

    /// <summary>
    /// Creates a token signed with the default key.
    /// </summary>
    /// <param name="inviteId">The invite ID that the token names.</param>
    /// <param name="tokenSalt">The current salt of the invite.</param>
    /// <returns>The token string.</returns>
    string CreateToken(Guid inviteId, byte[] tokenSalt);

    /// <summary>
    /// Reads the invite ID from a token without checking the signature. Use it only to find the invite row.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="inviteId">The invite ID when the token has the expected shape.</param>
    /// <returns>
    /// <see langword="true"/> when the token has the expected shape; otherwise <see langword="false"/>.
    /// </returns>
    bool TryReadInviteId(string token, out Guid inviteId);

    /// <summary>
    /// Checks the token signature against the invite salt. The comparison takes the same time for every signature.
    /// A configured older key still validates tokens that it signed.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="tokenSalt">The current salt of the invite.</param>
    /// <returns>
    /// <see langword="true"/> when the signature matches; <see langword="false"/> for every malformed or
    /// mismatched token. The method never throws.
    /// </returns>
    bool IsValid(string token, byte[] tokenSalt);
}
