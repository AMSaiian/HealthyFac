namespace HealthyFac.Auth.Infrastructure.Keycloak;

/// <summary>
/// Defines the validator messages for the Keycloak admin configuration.
/// </summary>
public static class KeycloakValidationMessages
{
    public const string BaseAddressInvalid = "The Keycloak base address must be an absolute http or https URI.";

    public const string RealmRequired = "The Keycloak realm is required.";

    public const string SpaClientIdRequired = "The Keycloak client ID of the web app is required.";

    public const string SpaBaseUrlInvalid = "The base URL of the web app must be an absolute http or https URI.";

    public const string SetPasswordLifespanInvalid =
        "The lifespan of the set-password link must be above zero seconds.";
}
