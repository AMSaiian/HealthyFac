namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

public sealed class ServiceTokenOptions
{
    public const string SectionName = "ServiceTokens";

    public required string TokenEndpoint { get; init; }

    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }

    /// <summary>
    /// The time in seconds before the token expiry at which the cache fetches a new token, so a request never leaves
    /// with a token that expires on the way. A margin at or above the token lifetime makes every call fetch a token.
    /// </summary>
    public int RefreshMarginSeconds { get; init; } = 30;
}
