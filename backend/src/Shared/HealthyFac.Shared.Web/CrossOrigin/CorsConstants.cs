namespace HealthyFac.Shared.Web.CrossOrigin;

/// <summary>
/// Defines the shared cross-origin policy name and the origin syntax.
/// </summary>
public static class CorsConstants
{
    public const string PolicyName = "HealthyFacDefault";
    public const string Wildcard = "*";
    public const string RootPath = "/";

    /// <summary>
    /// The longest time that a browser keeps a preflight answer, in seconds. Chromium caps the cache at 7200 seconds
    /// and ignores a larger value.
    /// </summary>
    public const int MaxPreflightMaxAgeSeconds = 7200;
}
