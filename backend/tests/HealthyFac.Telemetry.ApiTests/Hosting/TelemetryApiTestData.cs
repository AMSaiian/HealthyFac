using HealthyFac.Testing.Authentication;

namespace HealthyFac.Telemetry.ApiTests.Hosting;

public static class TelemetryApiTestData
{
    public const string QueueNamePrefix = "telemetry-api-tests-";
    public const string InsightsClientId = "healthyfac-insights";
    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";

    public static IReadOnlyDictionary<string, string?> CreateSettings(
        string databaseConnection,
        string brokerConnection
    )
        => new Dictionary<string, string?>(TestTokenFactory.JwtSettings)
        {
            [key: "ConnectionStrings:Database"] = databaseConnection,
            [key: "ConnectionStrings:RabbitMq"] = brokerConnection,
            [key: "Messaging:QueueName"] = $"{QueueNamePrefix}{Guid.NewGuid():N}",
            [key: "IdEncryption:Keys:0:Prefix"] = "v1.",
            [key: "IdEncryption:Keys:0:Key"] = IdEncryptionKey,
            [key: "IdEncryption:Keys:0:IsDefault"] = bool.TrueString,
            [key: "Seeding:IsOptionalDataSeeded"] = bool.FalseString,
            [key: "ServiceActors:AllowedClients:0"] = InsightsClientId,
        };
}
