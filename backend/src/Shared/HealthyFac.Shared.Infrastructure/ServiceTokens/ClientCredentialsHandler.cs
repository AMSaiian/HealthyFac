using System.Net.Http.Headers;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Sets the access token of the service itself on every outgoing request. When the token request fails, the handler
/// throws and sends nothing, so a call never leaves without its token.
/// </summary>
/// <param name="tokenCache">Provides the cached token.</param>
public sealed class ClientCredentialsHandler(ServiceTokenCache tokenCache) : DelegatingHandler
{
    private readonly ServiceTokenCache _tokenCache = tokenCache;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var token = await _tokenCache.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(ServiceTokenConstants.BearerScheme, token);

        return await base.SendAsync(request, cancellationToken);
    }
}
