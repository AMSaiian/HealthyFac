using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace HealthyFac.Gateway.Web.Realtime;

/// <summary>
/// Reads the access token from the query string on the hub path only. A browser WebSocket cannot send an
/// <c>Authorization</c> header. Every other path ignores the query, so a token in a URL never authenticates a
/// proxied call.
/// </summary>
public sealed class RealtimeTokenReader : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var previous = options.Events.OnMessageReceived;

        options.Events.OnMessageReceived = async context =>
        {
            await previous(context);

            var isHubRequest = context.Request.Path.StartsWithSegments(RealtimeConstants.HubPath);
            var token = context.Request.Query[RealtimeConstants.AccessTokenQueryKey].ToString();

            if (isHubRequest && !string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
        };
    }
}
