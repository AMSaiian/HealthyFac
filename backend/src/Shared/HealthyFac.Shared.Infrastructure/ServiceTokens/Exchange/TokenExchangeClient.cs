using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Exchanges the access token of a user for a short token of one target audience, and keeps the exchanged tokens
/// until they near their expiry.
/// </summary>
/// <remarks>
/// One instance serves the whole process. The tokens stay in the <see cref="TokenStore"/> in Redis, shared by the
/// instances of the service, with one entry for each user, audience, and scope set.
/// </remarks>
/// <param name="endpointClient">Requests the tokens from the token endpoint.</param>
/// <param name="store">Keeps the tokens in Redis.</param>
/// <param name="serviceOptions">Holds the name of the running service for the keys.</param>
public sealed class TokenExchangeClient(
    TokenEndpointClient endpointClient,
    TokenStore store,
    IOptions<ServiceOptions> serviceOptions
)
{
    private readonly TokenEndpointClient _endpointClient = endpointClient;
    private readonly TokenStore _store = store;
    private readonly string _serviceName = serviceOptions.Value.Name;

    /// <summary>
    /// Returns the stored exchanged token of the user, or exchanges the subject token when the store has none or the
    /// token has passed its refresh moment: <see cref="ServiceTokenOptions.RefreshMarginSeconds"/> plus a random jitter
    /// of up to <see cref="ServiceTokenOptions.RefreshJitterSeconds"/> before its expiry.
    /// </summary>
    /// <param name="userId">The ID of the user. It is part of the key, so two users never share a token.</param>
    /// <param name="subjectToken">The access token of the user.</param>
    /// <param name="audience">The client ID of the target service.</param>
    /// <param name="scope">The requested scopes, separated by spaces. The exchange always adds
    /// <see cref="ServiceTokenConstants.OrganizationScope"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The exchanged access token.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the token endpoint answers a failure status (see <see cref="HttpResponseMessageExtensions"/>),
    /// for example for a disabled user or an audience that the calling client may not request.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response holds no access token (see <see cref="ServiceTokenErrors.TokenResponseInvalid"/>).
    /// </exception>
    public Task<string> GetTokenAsync(
        Guid userId,
        string subjectToken,
        string audience,
        string scope,
        CancellationToken cancellationToken
    )
    {
        var scopes = scope.Split(
                ServiceTokenConstants.ScopeSeparator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Append(ServiceTokenConstants.OrganizationScope)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var key = TokenCacheKeys.ForExchangedToken(
            _serviceName,
            userId,
            audience,
            string.Join(ServiceTokenConstants.ScopeSeparator, scopes.Order(StringComparer.Ordinal)));

        return _store.GetOrFetchAsync(
            key,
            token => ExchangeAsync(subjectToken, audience, scopes, token),
            cancellationToken);
    }

    private Task<CachedToken> ExchangeAsync(
        string subjectToken,
        string audience,
        List<string> scopes,
        CancellationToken cancellationToken
    ) =>
        _endpointClient.SendAsync(
            new Dictionary<string, string>
            {
                [ServiceTokenConstants.Form.GrantType] = ServiceTokenConstants.Form.TokenExchangeGrant,
                [ServiceTokenConstants.Form.SubjectToken] = subjectToken,
                [ServiceTokenConstants.Form.SubjectTokenType] = ServiceTokenConstants.Form.AccessTokenType,
                [ServiceTokenConstants.Form.Audience] = audience,
                [ServiceTokenConstants.Form.Scope] = string.Join(ServiceTokenConstants.ScopeSeparator, scopes),
            },
            cancellationToken);
}
