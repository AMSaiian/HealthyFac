using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Net.Http.Headers;
using SharedCorsConstants = HealthyFac.Shared.Web.CrossOrigin.CorsConstants;

namespace HealthyFac.Gateway.Web.Realtime;

/// <summary>
/// Rejects a hub connection whose <c>Origin</c> header is missing or not allowed by the shared CORS policy
/// (<see cref="SharedCorsConstants.PolicyName"/>). A browser always sends the header, and a WebSocket handshake is not
/// subject to CORS, so this check stops a foreign page from opening a connection.
/// <c>WebSocketOptions.AllowedOrigins</c> does not cover a missing header, so it cannot replace this check. A request
/// to another path passes.
/// </summary>
/// <param name="policyProvider">Resolves the shared CORS policy, so one definition of "allowed origin" exists.</param>
public sealed class RealtimeOriginCheck(ICorsPolicyProvider policyProvider) : IMiddleware
{
    private readonly ICorsPolicyProvider _policyProvider = policyProvider;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Path.StartsWithSegments(RealtimeConstants.HubPath))
        {
            await next(context);

            return;
        }

        var isAllowed = await IsAllowedAsync(context);
        if (!isAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            return;
        }

        await next(context);
    }

    private async Task<bool> IsAllowedAsync(HttpContext context)
    {
        var origin = context.Request.Headers[HeaderNames.Origin].ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return false;
        }

        var policy = await _policyProvider.GetPolicyAsync(context, SharedCorsConstants.PolicyName);

        return policy is not null && policy.IsOriginAllowed(origin);
    }
}
