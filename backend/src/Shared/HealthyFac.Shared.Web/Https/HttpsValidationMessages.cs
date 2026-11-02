using System.Net;

namespace HealthyFac.Shared.Web.Https;

/// <summary>
/// Defines the validator messages for the transport security configuration.
/// </summary>
public static class HttpsValidationMessages
{
    public const string RedirectPortRequired = "A redirect port is required when HTTPS redirection is enabled.";
    public const string MaxAgeMustBePositive = "The HSTS max age must be at least 1 day.";

    public static readonly string PreloadRequirementsNotMet =
        "HSTS preload needs HSTS enabled, subdomains included, and a max age of at least "
        + $"{HttpsConstants.PreloadMinimumMaxAgeDays} days.";

    public static readonly string PortOutOfRange =
        $"The redirect port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}.";
}
