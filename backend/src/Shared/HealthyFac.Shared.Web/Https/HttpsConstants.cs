namespace HealthyFac.Shared.Web.Https;

/// <summary>
/// Defines the limits that the transport security validator enforces.
/// </summary>
public static class HttpsConstants
{
    /// <summary>The smallest HSTS max age that browser preload lists accept.</summary>
    public const int PreloadMinimumMaxAgeDays = 365;
}
