namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Defines the validator messages for the service token configuration.
/// </summary>
public static class ServiceTokenValidationMessages
{
    public const string TokenEndpointInvalid = "The service token endpoint must be an absolute http or https URI.";

    public const string ClientIdRequired = "The service token client ID is required.";

    public const string ClientSecretRequired = "The service token client secret is required.";

    public const string RefreshMarginInvalid = "The service token refresh margin must not be below 0 seconds.";
}
