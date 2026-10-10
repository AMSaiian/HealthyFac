namespace HealthyFac.Shared.Web.CrossOrigin;

/// <summary>
/// Configures browser cross-origin access for a single service.
/// Leave <see cref="Enabled"/> false wherever a gateway owns the policy. Two layers emitting the headers
/// produce a duplicate <c>Access-Control-Allow-Origin</c>, which every browser rejects.
/// </summary>
public sealed class CorsPolicyOptions
{
    public const string SectionName = "Cors";

    /// <summary>Set to true only where this service answers browsers directly.</summary>
    public bool Enabled { get; init; }

    /// <summary>The browser origins allowed to call this service, for example "http://localhost:4200".</summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Allows the browser to send cookies and TLS client certificates. Leave this false for bearer token
    /// authentication: the <c>Authorization</c> header is an ordinary header to CORS, covered by
    /// <c>AllowAnyHeader</c>, and is not a credential. Enable it only for a cookie session, such as a
    /// backend for frontend. Enabling it also forbids the wildcard origin.
    /// </summary>
    public bool AllowCredentials { get; init; }
}
