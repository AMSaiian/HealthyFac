namespace HealthyFac.Seeder.Timeline;

/// <summary>A username and a password that a seed event sends to Keycloak. The check applies the realm rules.</summary>
public sealed class SeedCredential
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}
