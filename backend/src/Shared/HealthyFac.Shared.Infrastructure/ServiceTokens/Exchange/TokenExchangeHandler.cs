using System.Net.Http.Headers;
using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens.Exchange;

/// <summary>
/// Exchanges the token of the user of the current request for a token of one target audience and sets it on every
/// outgoing request. When the exchange fails or the flow has no user token, the handler throws and sends nothing, so
/// a call never leaves without a token or with a token of another kind.
/// </summary>
/// <remarks>
/// An exchanged token is used only inside a user request that passed the gateway revocation check, so the target
/// needs no revocation check of its own. Keycloak refuses the exchange for a disabled user.
/// The token carries the permissions of the user, including the client roles of the calling client, because Keycloak
/// keeps them in its exchanged tokens also with <c>fullScopeAllowed: false</c>. The downstream scopes do not narrow
/// it.
/// Asynchronous work (a message handler, a job) has no user token. It uses Client Credentials and never
/// attempts an exchange.
/// </remarks>
/// <param name="accessor">Provides the token and the ID of the user of the current request.</param>
/// <param name="client">Exchanges and caches the tokens.</param>
/// <param name="audience">The client ID of the target service.</param>
/// <param name="scope">The scopes to request, separated by spaces.</param>
public sealed class TokenExchangeHandler(
    IIncomingAccessTokenAccessor accessor,
    TokenExchangeClient client,
    string audience,
    string scope
) : DelegatingHandler
{
    private readonly IIncomingAccessTokenAccessor _accessor = accessor;
    private readonly TokenExchangeClient _client = client;
    private readonly string _audience = audience;
    private readonly string _scope = scope;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var accessToken = _accessor.TryGetUserAccessToken();
        var userId = _accessor.TryGetUserId();
        if (accessToken is null || userId is null)
        {
            throw ServiceTokenErrors.NoIncomingUserToken();
        }

        var token = await _client.GetTokenAsync(userId.Value, accessToken, _audience, _scope, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(ServiceTokenConstants.BearerScheme, token);

        return await base.SendAsync(request, cancellationToken);
    }
}
