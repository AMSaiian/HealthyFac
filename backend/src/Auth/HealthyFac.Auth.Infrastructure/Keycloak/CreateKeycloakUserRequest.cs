using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed record CreateKeycloakUserRequest
{
    public required string Username { get; init; }

    [SensitiveData]
    public required string Email { get; init; }

    public required bool EmailVerified { get; init; }

    public required bool Enabled { get; init; }

    [SensitiveData]
    public required string FirstName { get; init; }

    [SensitiveData]
    public required string LastName { get; init; }

    public required IReadOnlyDictionary<string, string[]> Attributes { get; init; }
}
