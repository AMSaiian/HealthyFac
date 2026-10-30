namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed record KeycloakCredential
{
    public required string Type { get; init; }
}
