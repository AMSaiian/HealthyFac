namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// An access token that the token endpoint just issued, with the moment at which a holder replaces it and the moment
/// at which it expires.
/// </summary>
internal sealed record CachedToken
{
    public required string Value { get; init; }

    /// <summary>
    /// The moment at which the token stops being usable: the refresh margin plus a random jitter before the expiry.
    /// </summary>
    public required DateTimeOffset RefreshAt { get; init; }

    /// <summary>The moment at which the token endpoint stops accepting the token.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
