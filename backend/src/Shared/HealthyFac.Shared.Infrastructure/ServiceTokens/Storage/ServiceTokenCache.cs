using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;

/// <summary>
/// Holds the access tokens of the service itself, so every outgoing call reuses a token until it nears its expiry.
/// </summary>
/// <remarks>
/// One instance serves the whole process. The tokens stay in the <see cref="TokenStore"/> in Redis, shared by the
/// instances of the service, with one entry for each set of requested scopes: a flow that asks for other downstream
/// scopes gets another token. The store lets one caller fetch while the others wait and then read the new token, so
/// parallel calls, also across instances, make one token request. A failed fetch stores nothing, and the next call
/// tries again.
/// </remarks>
/// <param name="endpointClient">Requests the tokens from the token endpoint.</param>
/// <param name="store">Keeps the tokens in Redis.</param>
/// <param name="serviceOptions">Holds the name of the running service for the keys.</param>
public sealed class ServiceTokenCache(
    TokenEndpointClient endpointClient,
    TokenStore store,
    IOptions<ServiceOptions> serviceOptions
)
{
    private readonly TokenEndpointClient _endpointClient = endpointClient;
    private readonly TokenStore _store = store;
    private readonly string _serviceName = serviceOptions.Value.Name;

    /// <summary>
    /// Returns the stored token for the scopes, or fetches a new one when the store has none or the token has passed
    /// its refresh moment: <see cref="ServiceTokenOptions.RefreshMarginSeconds"/> plus a random jitter of up to
    /// <see cref="ServiceTokenOptions.RefreshJitterSeconds"/> before its expiry.
    /// </summary>
    /// <param name="scopes">The scopes to request. The order and repeated names do not matter, so two orders of the
    /// same scopes share one token. No scope gives a token without the permissions of any downstream scope.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The access token.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the token endpoint answers a failure status (see <see cref="HttpResponseMessageExtensions"/>).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the response holds no access token (see <see cref="ServiceTokenErrors.TokenResponseInvalid"/>).
    /// </exception>
    public Task<string> GetTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken)
    {
        var requested = scopes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        return _store.GetOrFetchAsync(
            TokenCacheKeys.ForServiceToken(_serviceName, string.Join(ServiceTokenConstants.ScopeSeparator, requested)),
            token => FetchAsync(requested, token),
            cancellationToken);
    }

    private Task<CachedToken> FetchAsync(List<string> scopes, CancellationToken cancellationToken)
    {
        var grantFields = new Dictionary<string, string>
        {
            [ServiceTokenConstants.Form.GrantType] = ServiceTokenConstants.Form.ClientCredentialsGrant,
        };
        if (scopes.Count > 0)
        {
            grantFields[ServiceTokenConstants.Form.Scope] = string.Join(ServiceTokenConstants.ScopeSeparator, scopes);
        }

        return _endpointClient.SendAsync(grantFields, cancellationToken);
    }
}
