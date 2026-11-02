using System.Globalization;

namespace HealthyFac.Core.Web.Employees.Emails;

/// <summary>
/// Formats values for the invite email. Both bodies use it, so they show the same text.
/// </summary>
public static class EmployeeInviteEmailFormat
{
    private const string ExpiryPattern = "yyyy-MM-dd HH:mm";
    private const string UtcSuffix = "UTC";

    /// <summary>
    /// Formats the expiry time with an explicit UTC suffix, because the time zone of the recipient is unknown.
    /// </summary>
    /// <param name="expiresAt">The UTC expiry time.</param>
    /// <returns>The text, for example <c>2026-11-03 14:30 UTC</c>.</returns>
    public static string Expiry(DateTime expiresAt) =>
        $"{expiresAt.ToString(ExpiryPattern, CultureInfo.InvariantCulture)} {UtcSuffix}";
}
