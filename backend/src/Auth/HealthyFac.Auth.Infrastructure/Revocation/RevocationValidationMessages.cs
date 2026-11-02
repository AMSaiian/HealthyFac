namespace HealthyFac.Auth.Infrastructure.Revocation;

/// <summary>
/// Defines the validator messages for the revocation configuration.
/// </summary>
public static class RevocationValidationMessages
{
    public const string TokenLifetimeInvalid = "The access token lifetime must be above zero seconds.";

    public const string ClockSkewInvalid = "The clock skew must not be below zero seconds.";
}
