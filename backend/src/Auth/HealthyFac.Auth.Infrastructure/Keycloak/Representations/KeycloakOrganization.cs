namespace HealthyFac.Auth.Infrastructure.Keycloak.Representations;

public sealed record KeycloakOrganization
{
    public required Guid Id { get; init; }
}
