using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Sets the actor headers of the calling flow on every outgoing request, so the called service restores the actor
/// with the same rule as the inbox. Use it only for a client that calls one of our own services with a service
/// token, never for the Keycloak client or an external API.
/// </summary>
/// <remarks>
/// A request that has the token of a user must use the token exchange: the user token then carries the user and the
/// permissions, and a service token with actor headers would replace it. The handler throws in that case and sends
/// nothing. Asynchronous work (an inbox handler, a job) has no user token, so it sends its actor as it is, a
/// restored user included. The actor comes from the flow that entered it into <see cref="ActorContextHolder"/>,
/// because a handler of <c>IHttpClientFactory</c> lives in its own DI scope.
/// </remarks>
/// <param name="accessor">Tells whether the current request has a user token.</param>
public sealed class ActorHeadersHandler(IIncomingAccessTokenAccessor accessor) : DelegatingHandler
{
    private readonly IIncomingAccessTokenAccessor _accessor = accessor;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (_accessor.TryGetUserAccessToken() is not null)
        {
            throw ServiceTokenErrors.ActorHeadersNeedTokenExchange();
        }

        var actor = ActorContextHolder.FlowActor ?? throw ServiceTokenErrors.ActorHeadersWithoutActor();
        SetHeader(request, ActorHeaderNames.ActorId, actor.UserId.ToString());
        SetHeader(request, ActorHeaderNames.CompanyId, actor.CompanyId?.ToString());

        return base.SendAsync(request, cancellationToken);
    }

    private static void SetHeader(HttpRequestMessage request, string name, string? value)
    {
        request.Headers.Remove(name);
        if (value is not null)
        {
            request.Headers.Add(name, value);
        }
    }
}
