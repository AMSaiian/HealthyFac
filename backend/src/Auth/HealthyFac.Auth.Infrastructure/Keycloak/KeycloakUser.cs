namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed record KeycloakUser
{
    public required Guid Id { get; init; }
}
