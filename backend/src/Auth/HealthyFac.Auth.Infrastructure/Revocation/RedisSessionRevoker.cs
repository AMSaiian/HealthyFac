using HealthyFac.Auth.Application.Identities;
using HealthyFac.Shared.Infrastructure.Revocation;
using Microsoft.Extensions.Options;

namespace HealthyFac.Auth.Infrastructure.Revocation;

/// <summary>
/// Writes the revocation entry of a user to the Redis list that the gateway reads. The entry lives for the token
/// lifetime plus the clock skew (<see cref="RevocationOptions"/>). A Redis failure propagates, so the caller retries.
/// </summary>
/// <param name="store">The shared revocation list.</param>
/// <param name="options">Holds the token lifetime and the clock skew.</param>
public sealed class RedisSessionRevoker(ITokenRevocationStore store, IOptions<RevocationOptions> options)
    : ISessionRevoker
{
    private readonly ITokenRevocationStore _store = store;
    private readonly IOptions<RevocationOptions> _options = options;

    public Task RevokeAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        => _store.RevokeAsync(
            userId,
            revokedAt,
            TimeSpan.FromSeconds(_options.Value.TokenLifetimeSeconds + _options.Value.ClockSkewSeconds),
            cancellationToken);
}
