using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Posts a grant to the token endpoint of the calling client and reads the token from the answer. The Client
/// Credentials cache and the token exchange client share it, so both read one answer format.
/// </summary>
/// <remarks>
/// The client is a singleton and creates the named HTTP client <see cref="ServiceTokenConstants.TokenClientName"/>
/// for each call. A typed client in a singleton would keep one handler forever, so <c>IHttpClientFactory</c> would
/// never rotate it. The refresh moment of each token is the expiry minus the refresh margin minus a random jitter of
/// up to <see cref="ServiceTokenOptions.RefreshJitterSeconds"/>, so the instances of a service do not ask Keycloak
/// for new tokens at the same moment.
/// </remarks>
/// <param name="httpClientFactory">Creates the client of the token endpoint. That client has no token handler.</param>
/// <param name="options">Holds the token endpoint, the client credentials, the refresh margin, and the refresh
/// jitter.</param>
/// <param name="timeProvider">Provides the current time.</param>
/// <param name="random">Provides the refresh jitter of each token.</param>
public sealed class TokenEndpointClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceTokenOptions> options,
    TimeProvider timeProvider,
    Random random
)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ServiceTokenOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly Random _random = random;

    /// <summary>
    /// Sends the grant fields together with the client credentials. The form never enters an exception text.
    /// </summary>
    /// <param name="grantFields">The form fields of the grant, without the client credentials.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The token, the moment at which it must be replaced, and the moment at which it expires.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the token endpoint answers a failure status (see <see cref="HttpResponseMessageExtensions"/>).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response holds no access token (see <see cref="ServiceTokenErrors.TokenResponseInvalid"/>).
    /// </exception>
    internal async Task<CachedToken> SendAsync(
        IReadOnlyDictionary<string, string> grantFields,
        CancellationToken cancellationToken
    )
    {
        var form = new Dictionary<string, string>(grantFields)
        {
            [ServiceTokenConstants.Form.ClientId] = _options.ClientId,
            [ServiceTokenConstants.Form.ClientSecret] = _options.ClientSecret,
        };
        using var content = new FormUrlEncodedContent(form);
        using var client = _httpClientFactory.CreateClient(ServiceTokenConstants.TokenClientName);

        using var response = await client.PostAsync(_options.TokenEndpoint, content, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (body is null || string.IsNullOrWhiteSpace(body.AccessToken) || body.ExpiresInSeconds <= 0)
        {
            throw ServiceTokenErrors.TokenResponseInvalid();
        }

        var expiresAt = _timeProvider.GetUtcNow() + TimeSpan.FromSeconds(body.ExpiresInSeconds);

        return new CachedToken
        {
            Value = body.AccessToken,
            RefreshAt = expiresAt
                - TimeSpan.FromSeconds(_options.RefreshMarginSeconds)
                - TimeSpan.FromSeconds(_random.NextDouble() * _options.RefreshJitterSeconds),
            ExpiresAt = expiresAt,
        };
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName(ServiceTokenConstants.Response.AccessToken)]
        public string? AccessToken { get; init; }

        [JsonPropertyName(ServiceTokenConstants.Response.ExpiresIn)]
        public int ExpiresInSeconds { get; init; }
    }
}
