using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Auth.Infrastructure.Keycloak.Representations;

public sealed record CreateKeycloakUserRequest
{
    public required string Username { get; init; }

    [SensitiveData]
    public required string Email { get; init; }

    [JsonPropertyName(name: "emailVerified")]
    public required bool IsEmailVerified { get; init; }

    [JsonPropertyName(name: "enabled")]
    public required bool IsEnabled { get; init; }

    [SensitiveData]
    public required string FirstName { get; init; }

    [SensitiveData]
    public required string LastName { get; init; }

    public required IReadOnlyDictionary<string, string[]> Attributes { get; init; }
}
