using System.Net.Http.Headers;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Sets the access token of the service itself on every outgoing request. The token carries only the permissions of
/// the downstream scopes of the current flow: the handler asks Keycloak for the scope `{service}-{name}` of each
/// downstream scope, and a flow without a downstream scope gets a token without a permission. When the token
/// request fails, the handler throws and sends nothing, so a call never leaves without its token.
/// </summary>
/// <param name="tokenCache">Provides the cached token for a set of scopes.</param>
/// <param name="downstreamScopes">Holds the downstream scopes of the current flow.</param>
/// <param name="serviceOptions">Holds the name of the running service, which prefixes the downstream scope
/// names.</param>
public sealed class ClientCredentialsHandler(
    ServiceTokenCache tokenCache,
    IDownstreamScopeHolder downstreamScopes,
    IOptions<ServiceOptions> serviceOptions
) : DelegatingHandler
{
    private readonly ServiceTokenCache _tokenCache = tokenCache;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;
    private readonly IOptions<ServiceOptions> _serviceOptions = serviceOptions;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var scopes = DownstreamScopeNames.Prefix(_serviceOptions.Value.Name, _downstreamScopes.Current);
        var token = await _tokenCache.GetTokenAsync(scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(ServiceTokenConstants.BearerScheme, token);

        return await base.SendAsync(request, cancellationToken);
    }
}
