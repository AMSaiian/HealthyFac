namespace HealthyFac.Shared.Web.Authentication;

/// <summary>
/// Defines the validator messages for the JWT authentication configuration.
/// </summary>
public static class AuthenticationValidationMessages
{
    public const string IssuerInvalid = "The issuer must be an absolute http or https URL.";
    public const string MetadataAddressInvalid = "The metadata address must be an absolute http or https URL.";
    public const string AudienceMissing = "The audience must not be empty.";
}
