using HealthyFac.Shared.Web.CrossOrigin;
using Microsoft.Extensions.Options;

namespace HealthyFac.Gateway.Web.Realtime;

/// <summary>
/// Gives the WebSocket middleware the origins of <see cref="CorsPolicyOptions.AllowedOrigins"/>, so one list defines
/// "allowed origin". A WebSocket handshake is not subject to CORS, so the middleware checks the <c>Origin</c> header
/// itself: a request with another origin gets <c>403</c>. A request without an <c>Origin</c> header passes, because a
/// client that is not a browser can send any value. An empty list or a <c>*</c> entry allows every origin.
/// </summary>
/// <param name="corsOptions">The shared CORS options.</param>
public sealed class WebSocketOriginOptionsSetup(IOptions<CorsPolicyOptions> corsOptions)
    : IConfigureOptions<WebSocketOptions>
{
    private readonly IOptions<CorsPolicyOptions> _corsOptions = corsOptions;

    public void Configure(WebSocketOptions options)
    {
        foreach (var origin in _corsOptions.Value.AllowedOrigins)
        {
            options.AllowedOrigins.Add(origin);
        }
    }
}
