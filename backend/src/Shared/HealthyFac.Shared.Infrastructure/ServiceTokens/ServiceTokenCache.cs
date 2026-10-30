using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Holds the access token of the service itself, so every outgoing call reuses one token until it nears its expiry.
/// </summary>
/// <remarks>
/// One instance serves the whole process. A semaphore lets one caller fetch while the others wait and then read the
/// new token, so parallel calls make one token request. A failed fetch stores nothing, and the next call tries again.
/// </remarks>
/// <param name="httpClientFactory">Creates the client of the token endpoint.</param>
/// <param name="options">Holds the token endpoint and the client credentials.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class ServiceTokenCache(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceTokenOptions> options,
    TimeProvider timeProvider
)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ServiceTokenOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly SemaphoreSlim _fetchGate = new(1, 1);

    private volatile CachedToken? _cachedToken;

    /// <summary>
    /// Returns the cached token, or fetches a new one when the cache is empty or the token is within
    /// <see cref="ServiceTokenOptions.RefreshMarginSeconds"/> of its expiry.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The access token.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the token endpoint answers a failure status (see
    /// <see cref="ServiceTokenErrors.TokenRequestFailed"/>).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response holds no access token (see <see cref="ServiceTokenErrors.TokenResponseInvalid"/>).
    /// </exception>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var cached = _cachedToken;
        if (IsUsable(cached))
        {
            return cached.Value;
        }

        await _fetchGate.WaitAsync(cancellationToken);
        try
        {
            cached = _cachedToken;
            if (IsUsable(cached))
            {
                return cached.Value;
            }

            var fetched = await FetchAsync(cancellationToken);
            _cachedToken = fetched;

            return fetched.Value;
        }
        finally
        {
            _fetchGate.Release();
        }
    }

    private bool IsUsable([NotNullWhen(true)] CachedToken? token) =>
        token is not null && _timeProvider.GetUtcNow() < token.RefreshAt;

    private async Task<CachedToken> FetchAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(ServiceTokenConstants.TokenClientName);
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                [ServiceTokenConstants.Form.GrantType] = ServiceTokenConstants.Form.ClientCredentialsGrant,
                [ServiceTokenConstants.Form.ClientId] = _options.ClientId,
                [ServiceTokenConstants.Form.ClientSecret] = _options.ClientSecret,
            });

        using var response = await client.PostAsync(_options.TokenEndpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw ServiceTokenErrors.TokenRequestFailed(response.StatusCode);
        }

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (body is null || string.IsNullOrWhiteSpace(body.AccessToken) || body.ExpiresInSeconds <= 0)
        {
            throw ServiceTokenErrors.TokenResponseInvalid();
        }

        var lifetime = TimeSpan.FromSeconds(body.ExpiresInSeconds);

        return new CachedToken(
            body.AccessToken,
            _timeProvider.GetUtcNow() + lifetime - TimeSpan.FromSeconds(_options.RefreshMarginSeconds));
    }

    private sealed record CachedToken(string Value, DateTimeOffset RefreshAt);

    private sealed record TokenResponse
    {
        [JsonPropertyName(ServiceTokenConstants.Response.AccessToken)]
        public string? AccessToken { get; init; }

        [JsonPropertyName(ServiceTokenConstants.Response.ExpiresIn)]
        public int ExpiresInSeconds { get; init; }
    }
}
