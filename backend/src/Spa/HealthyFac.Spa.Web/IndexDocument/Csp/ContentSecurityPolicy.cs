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
    /// </remarks>
    /// <param name="nonce">The nonce of the response. Angular applies it to its own style elements.</param>
    /// <param name="apiBaseUrl">The public URL of the API gateway. The policy allows only its origin.</param>
    /// <param name="authUrl">The public URL of the identity provider. The policy allows only its origin.</param>
    /// <returns>The header value.</returns>
    public static string Build(string nonce, Uri apiBaseUrl, Uri authUrl)
    {
        var authOrigin = authUrl.GetLeftPart(UriPartial.Authority);

        return $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src-elem 'self' 'nonce-{nonce}'; "
            + "style-src-attr 'unsafe-inline'; img-src 'self' data:; font-src 'self'; "
            + $"connect-src 'self' {apiBaseUrl.GetLeftPart(UriPartial.Authority)} {authOrigin}; "
            + $"frame-src 'self' {authOrigin}; base-uri 'self'; form-action 'self'; "
            + "frame-ancestors 'none'; object-src 'none'";
    }
}
