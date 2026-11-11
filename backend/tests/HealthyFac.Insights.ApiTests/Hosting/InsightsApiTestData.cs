using HealthyFac.Testing.Authentication;

namespace HealthyFac.Insights.ApiTests.Hosting;

public static class InsightsApiTestData
{
    public const string QueueNamePrefix = "insights-api-tests-";

    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";

    public static IReadOnlyDictionary<string, string?> CreateSettings(
        string databaseConnection,
        string brokerConnection,
        string redisConnection
    )
        => new Dictionary<string, string?>(TestTokenFactory.JwtSettings)
        {
            [key: "ConnectionStrings:Database"] = databaseConnection,
            [key: "ConnectionStrings:RabbitMq"] = brokerConnection,
            [key: "Redis:ConnectionString"] = redisConnection,
            [key: "Messaging:QueueName"] = $"{QueueNamePrefix}{Guid.NewGuid():N}",
            [key: "ServiceTokens:TokenEndpoint"] = "http://localhost:1/token",
            [key: "ServiceTokens:ClientId"] = "healthyfac-insights",
            [key: "ServiceTokens:ClientSecret"] = "unused-in-tests",
            [key: "DocumentClient:GrpcAddress"] = "http://localhost:1",
            [key: "TelemetryClient:BaseAddress"] = "http://localhost:1",
            [key: "Docling:BaseUrl"] = "http://localhost:1",
            [key: "Docling:ApiKey"] = "unused-in-tests",
            [key: "Gemini:ProjectId"] = "test-project",
            [key: "Gemini:Location"] = "test-location",
            [key: "Gemini:ChatModel"] = "test-chat-model",
            [key: "Ingest:IsWorkerEnabled"] = bool.FalseString,
            [key: "IdEncryption:Keys:0:Prefix"] = "v1.",
            [key: "IdEncryption:Keys:0:Key"] = IdEncryptionKey,
            [key: "IdEncryption:Keys:0:IsDefault"] = bool.TrueString,
        };
}
