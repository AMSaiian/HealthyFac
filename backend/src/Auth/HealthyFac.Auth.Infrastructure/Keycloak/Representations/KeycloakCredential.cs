namespace HealthyFac.Auth.Infrastructure.Keycloak.Representations;

public sealed record KeycloakCredential
{
    public required string Type { get; init; }
}
