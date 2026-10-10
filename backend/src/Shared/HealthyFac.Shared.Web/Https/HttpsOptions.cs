namespace HealthyFac.Shared.Web.Https;

/// <summary>
/// Configures transport security for the service.
/// Behind a gateway that terminates TLS, keep <see cref="RedirectEnabled"/> false and let the gateway
/// redirect. The service still sends HSTS, so browsers refuse plain HTTP on later visits.
/// </summary>
public sealed class HttpsOptions
{
    public const string SectionName = "Https";

    /// <summary>Redirects plain HTTP requests to HTTPS. Requires <see cref="RedirectPort"/>.</summary>
    public bool RedirectEnabled { get; init; }

    /// <summary>
    /// The HTTPS port to redirect to. Without it the middleware cannot build the target URL and
    /// silently passes the request through.
    /// </summary>
    public int? RedirectPort { get; init; }

    /// <summary>Sends the Strict-Transport-Security header. Browsers ignore it over plain HTTP.</summary>
    public bool HstsEnabled { get; init; } = true;

    /// <summary>How long a browser must keep using HTTPS for this host.</summary>
    public int HstsMaxAgeDays { get; init; } = 365;

    /// <summary>Applies the policy to every subdomain.</summary>
    public bool HstsIncludeSubDomains { get; init; } = true;

    /// <summary>
    /// Requests inclusion in the browser preload lists. Set this only when the whole domain, including
    /// every subdomain, serves HTTPS. Removal from a preload list takes months.
    /// </summary>
    public bool HstsPreload { get; init; }
}
