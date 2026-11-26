using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Realtime;

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
            ["ConnectionStrings:Database"] = databaseConnection,
            ["ConnectionStrings:RabbitMq"] = brokerConnection,
            ["Messaging:QueueName"] = $"{QueueNamePrefix}{Guid.NewGuid():N}",
            ["IdEncryption:Keys:0:Prefix"] = "v1_",
            ["IdEncryption:Keys:0:Key"] = IdEncryptionKey,
            ["IdEncryption:Keys:0:IsDefault"] = bool.TrueString,
            ["Redis:ConnectionString"] = RealtimeTestSettings.UnreachableRedis,
            ["ServiceActors:AllowedClients:0"] = InsightsClientId,
        };
}
