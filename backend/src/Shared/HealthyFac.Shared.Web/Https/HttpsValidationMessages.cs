using System.Net;

namespace HealthyFac.Shared.Web.Https;

/// <summary>
/// Defines the validator messages for the transport security configuration.
/// </summary>
public static class HttpsValidationMessages
{
    public const string RedirectPortRequired = "A redirect port is required when HTTPS redirection is enabled.";
    public const string MaxAgeMustBePositive = "The HSTS max age must be at least one day.";

    public const string PreloadRequirementsNotMet =
        "HSTS preload needs HSTS enabled, subdomains included, and a max age of at least 365 days.";

    public static readonly string PortOutOfRange =
        $"The redirect port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}.";
}
