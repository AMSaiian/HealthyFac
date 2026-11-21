namespace HealthyFac.Spa.Web.IndexDocument.Csp;

public static class ContentSecurityPolicy
{
    /// <summary>
    /// The policy of the silent SSO page. The page loads only its own script and only the app can frame it.
    /// </summary>
    public const string SilentSso = "default-src 'none'; script-src 'self'; frame-ancestors 'self'";

    /// <summary>
    /// Builds the content security policy of the index document.
    /// </summary>
    /// <remarks>
    /// <c>style-src-attr 'unsafe-inline'</c> is a written exception: PrimeNG and Angular set inline
    /// <c>style</c> attributes, and an attribute style cannot run script. <c>frame-src</c> allows the hidden
    /// iframe of the silent SSO check: it loads the identity provider, which then redirects back to the app.
    /// <c>img-src</c> allows the API origin, because the images of a rich text load from the file links of the
    /// gateway (<c>/api/document/blobs</c>) on that origin. <c>connect-src</c> also allows the WebSocket form of the
    /// API origin: the hub connection of the realtime client uses the <c>wss:</c> scheme, which an <c>https:</c>
    /// source does not match.
    /// </remarks>
    /// <param name="nonce">The nonce of the response. Angular applies it to its own style elements.</param>
    /// <param name="apiBaseUrl">The public URL of the API gateway. The policy allows only its origin for requests and
    /// for images.</param>
    /// <param name="authUrl">The public URL of the identity provider. The policy allows only its origin.</param>
    /// <returns>The header value.</returns>
    public static string Build(string nonce, Uri apiBaseUrl, Uri authUrl)
    {
        var authOrigin = authUrl.GetLeftPart(UriPartial.Authority);
        var apiOrigin = apiBaseUrl.GetLeftPart(UriPartial.Authority);
        var apiWebSocketOrigin = WebSocketOriginOf(apiBaseUrl);

        return $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src-elem 'self' 'nonce-{nonce}'; "
            + "style-src-attr 'unsafe-inline'; "
            + $"img-src 'self' data: {apiOrigin}; font-src 'self'; "
            + $"connect-src 'self' {apiOrigin} {apiWebSocketOrigin} {authOrigin}; "
            + $"frame-src 'self' {authOrigin}; base-uri 'self'; form-action 'self'; "
            + "frame-ancestors 'none'; object-src 'none'";
    }

    private static string WebSocketOriginOf(Uri apiBaseUrl)
    {
        var webSocketUri = new UriBuilder
        {
            Scheme = apiBaseUrl.Scheme == Uri.UriSchemeHttps ? Uri.UriSchemeWss : Uri.UriSchemeWs,
            Host = apiBaseUrl.Host,
            Port = apiBaseUrl.IsDefaultPort ? -1 : apiBaseUrl.Port,
        };

        return webSocketUri.Uri.GetLeftPart(UriPartial.Authority);
    }
}
