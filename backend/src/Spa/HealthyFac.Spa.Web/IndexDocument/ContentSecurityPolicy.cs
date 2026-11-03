namespace HealthyFac.Spa.Web.IndexDocument;

public static class ContentSecurityPolicy
{
    /// <summary>
    /// Builds the content security policy of the index document.
    /// </summary>
    /// <remarks>
    /// <c>style-src-attr 'unsafe-inline'</c> is a written exception: PrimeNG and Angular set inline
    /// <c>style</c> attributes, and an attribute style cannot run script. The authentication session adds the
    /// origin of the identity provider to <c>connect-src</c>.
    /// </remarks>
    /// <param name="nonce">The nonce of the response. Angular applies it to its own style elements.</param>
    /// <param name="apiBaseUrl">The public URL of the API gateway. The policy allows only its origin.</param>
    /// <returns>The header value.</returns>
    public static string Build(string nonce, Uri apiBaseUrl) =>
        $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src-elem 'self' 'nonce-{nonce}'; "
        + "style-src-attr 'unsafe-inline'; img-src 'self' data:; font-src 'self'; "
        + $"connect-src 'self' {apiBaseUrl.GetLeftPart(UriPartial.Authority)}; base-uri 'self'; form-action 'self'; "
        + "frame-ancestors 'none'; object-src 'none'";
}
