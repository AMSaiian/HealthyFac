namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed class KeycloakAdminOptions
{
    public const string SectionName = "KeycloakAdmin";

    public required string BaseAddress { get; init; }

    public required string Realm { get; init; }

    public required string SpaClientId { get; init; }

    public required string SpaBaseUrl { get; init; }

    public int SetPasswordLifespanSeconds { get; init; } = KeycloakAdminConstants.DefaultSetPasswordLifespanSeconds;
}
