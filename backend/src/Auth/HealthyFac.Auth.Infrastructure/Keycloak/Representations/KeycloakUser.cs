namespace HealthyFac.Auth.Infrastructure.Keycloak.Representations;

public sealed record KeycloakUser
{
    public required Guid Id { get; init; }
}
