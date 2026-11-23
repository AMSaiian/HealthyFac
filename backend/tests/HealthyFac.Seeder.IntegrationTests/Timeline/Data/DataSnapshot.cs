namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

public sealed record DataSnapshot
{
    public required IReadOnlyDictionary<string, string> Tables { get; init; }

    public required int Blobs { get; init; }

    public required int KeycloakUsers { get; init; }

    public required int Mails { get; init; }
}
