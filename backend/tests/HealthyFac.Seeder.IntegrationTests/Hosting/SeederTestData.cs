using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.IntegrationTests.Hosting;

public static class SeederTestData
{
    public const string RabbitMqUnused = "amqp://guest:guest@localhost:1";
    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";
    public const string InviteTokenSecret = "dGVzdC1pbnZpdGUtdG9rZW4ta2V5LTAxMjM0NTY3ODk=";
    public const string UnusedUrl = "http://localhost:1";
    public const string UnusedSecret = "unused-in-tests";
    public const string KeycloakRealm = "healthyfac";
    public const string SpaBaseUrl = "https://app.test";
    public const string PublicBlobBaseUrl = "https://api.healthyfac.test/api/document/blobs";

    public static readonly DateTimeOffset FirstMoment = new(
        year: 2027, month: 3, day: 24, hour: 7, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly ActorContext InitializationActor = new()
    {
        Kind = ActorKind.User,
        UserId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000a0ff"),
    };

    public static readonly IReadOnlyList<string> ServiceNames = ["core", "telemetry", "document", "insights", "auth"];
}
