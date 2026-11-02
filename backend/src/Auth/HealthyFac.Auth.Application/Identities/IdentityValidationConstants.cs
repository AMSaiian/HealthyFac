namespace HealthyFac.Auth.Application.Identities;

/// <summary>
/// Defines the limits of the identity data that the auth service accepts. The values equal the limits of the
/// employee in the core service, which sends the data.
/// </summary>
public static class IdentityValidationConstants
{
    /// <summary>The longest email address that SMTP allows (RFC 5321).</summary>
    public const int EmailMaxLength = 254;

    public const int NameMaxLength = 100;
}
