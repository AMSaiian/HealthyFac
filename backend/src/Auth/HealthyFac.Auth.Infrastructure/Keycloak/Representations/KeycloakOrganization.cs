namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed record KeycloakOrganization
{
    public required Guid Id { get; init; }
}
