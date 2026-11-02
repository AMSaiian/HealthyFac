using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// The JSON value of a token entry in the <see cref="ITokenCacheStorage"/>: the access token and the moment at
/// which a holder replaces it. The storage removes the entry at the expiry of the token.
/// </summary>
internal sealed record StoredToken
{
    public required string Token { get; init; }

    public required DateTimeOffset RefreshAt { get; init; }

    /// <summary>
    /// Reads a stored value.
    /// </summary>
    /// <param name="json">The value that <see cref="ToJson"/> wrote.</param>
    /// <returns>The token, or <see langword="null"/> when the value is not a valid entry. The caller then treats
    /// the entry as missing.</returns>
    public static StoredToken? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<StoredToken>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}
