using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Auth.Infrastructure.Keycloak;

public sealed record CreateKeycloakUserRequest
{
    public required string Username { get; init; }

    [SensitiveData]
    public required string Email { get; init; }

    [JsonPropertyName("emailVerified")]
    public required bool IsEmailVerified { get; init; }

    [JsonPropertyName("enabled")]
    public required bool IsEnabled { get; init; }

    [SensitiveData]
    public required string FirstName { get; init; }

    [SensitiveData]
    public required string LastName { get; init; }

    public required IReadOnlyDictionary<string, string[]> Attributes { get; init; }
}
