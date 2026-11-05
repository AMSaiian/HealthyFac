namespace HealthyFac.Spa.Web.Hosting;

/// <summary>
/// Configures the SPA host. Every value here is public in the browser or sets how the host serves files.
/// </summary>
public sealed class SpaOptions
{
    public const string SectionName = "Spa";

    /// <summary>
    /// The folder with the built app. A relative path starts at the application base directory.
    /// </summary>
    public required string ContentRoot { get; init; }

    /// <summary>
    /// The public URL of the API gateway. The host sends it to the browser in <c>/config.json</c> and allows it in
    /// the <c>connect-src</c> of the content security policy.
    /// </summary>
    public required string ApiBaseUrl { get; init; }

    /// <summary>
    /// The public URL of the identity provider (Keycloak). The host sends it to the browser in <c>/config.json</c>
    /// and allows its origin in the <c>connect-src</c> and <c>frame-src</c> of the content security policy.
    /// </summary>
    public required string AuthUrl { get; init; }

    /// <summary>
    /// The identity provider realm that the app signs in to.
    /// </summary>
    public required string AuthRealm { get; init; }

    /// <summary>
    /// The public client ID of the app in the identity provider realm.
    /// </summary>
    public required string AuthClientId { get; init; }

    /// <summary>
    /// The PrimeUI license key. PrimeNG checks it in the browser, so it is public by design. The host sends it to the
    /// browser in <c>/config.json</c>.
    /// </summary>
    public required string PrimeUiLicenseKey { get; init; }

    /// <summary>
    /// A .NET regular expression for the content hash in a file name (the Angular <c>outputHashing</c> part). A file
    /// that matches never changes, so it gets the immutable cache header.
    /// </summary>
    public required string HashedFileNamePattern { get; init; }

    /// <summary>
    /// The <c>max-age</c> of a hashed file in seconds.
    /// </summary>
    public required int ImmutableCacheSeconds { get; init; }

    /// <summary>
    /// The <c>max-age</c> of <c>/config.json</c> in seconds. A configuration change reaches open browsers within
    /// this time.
    /// </summary>
    public required int RuntimeConfigCacheSeconds { get; init; }

    /// <summary>
    /// The <c>Permissions-Policy</c> header value of the index document.
    /// </summary>
    public required string PermissionsPolicy { get; init; }
}
