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
    /// A .NET regular expression for the content hash in a file name (the Angular <c>outputHashing</c> part). A file
    /// that matches never changes, so it gets the immutable cache header.
    /// </summary>
    public required string HashedFileNamePattern { get; init; }

    /// <summary>
    /// The <c>max-age</c> of a hashed file in seconds.
    /// </summary>
    public required int ImmutableCacheSeconds { get; init; }

    /// <summary>
    /// The <c>Permissions-Policy</c> header value of the index document.
    /// </summary>
    public required string PermissionsPolicy { get; init; }
}
