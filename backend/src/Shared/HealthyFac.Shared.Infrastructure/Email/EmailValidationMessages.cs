using System.Net;

namespace HealthyFac.Shared.Infrastructure.Email;

/// <summary>
/// Defines the validator messages for the SMTP configuration.
/// </summary>
public static class EmailValidationMessages
{
    public const string HostRequired = "The SMTP host is required.";

    public const string FromAddressInvalid = "The SMTP from address must be a valid email address.";

    public const string CredentialsIncomplete = "The SMTP user name and password must both be set or both be empty.";

    public static readonly string PortOutOfRange =
        $"The SMTP port must be from {IPEndPoint.MinPort} to {IPEndPoint.MaxPort}.";

    public static readonly string TimeoutOutOfRange =
        $"The SMTP timeout must be from {EmailConstants.Timeout.MinSeconds} "
        + $"to {EmailConstants.Timeout.MaxSeconds} seconds.";
}
