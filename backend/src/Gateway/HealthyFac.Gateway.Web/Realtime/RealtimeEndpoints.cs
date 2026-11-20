using HealthyFac.Gateway.Web.RateLimiting;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.Http.Connections;

namespace HealthyFac.Gateway.Web.Realtime;

/// <summary>
/// Maps the real-time hub. The hub accepts WebSocket connections only, and it closes a connection when its token
/// expires.
/// </summary>
public static class RealtimeEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the hub on <see cref="RealtimeConstants.HubPath"/>. Call it before <c>MapReverseProxy</c>.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapRealtimeEndpoints()
        {
            app.MapHub<RealtimeHub>(
                    RealtimeConstants.HubPath,
                    options =>
                    {
                        options.Transports = HttpTransportType.WebSockets;
                        options.CloseOnAuthenticationExpiration = true;
                    })
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);

            return app;
        }
    }
}
