namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed record KeycloakRole
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}
