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
            ["ConnectionStrings:Database"] = databaseConnection,
            ["ConnectionStrings:RabbitMq"] = brokerConnection,
            ["Redis:ConnectionString"] = redisConnection,
            ["Messaging:QueueName"] = $"{QueueNamePrefix}{Guid.NewGuid():N}",
            ["ServiceTokens:TokenEndpoint"] = "http://localhost:1/token",
            ["ServiceTokens:ClientId"] = "healthyfac-insights",
            ["ServiceTokens:ClientSecret"] = "unused-in-tests",
            ["DocumentClient:GrpcAddress"] = "http://localhost:1",
            ["TelemetryClient:GrpcAddress"] = "http://localhost:1",
            ["Docling:BaseUrl"] = "http://localhost:1",
            ["CoreClient:GrpcAddress"] = "http://localhost:1",
            ["Docling:ApiKey"] = "unused-in-tests",
            ["Gemini:ProjectId"] = "test-project",
            ["Gemini:Location"] = "test-location",
            ["Gemini:ChatModel"] = "test-chat-model",
            ["Gemini:AssistantModel"] = "test-assistant-model",
            ["Gemini:TriageModel"] = "test-triage-model",
            ["Ingest:IsWorkerEnabled"] = bool.FalseString,
            ["IdEncryption:Keys:0:Prefix"] = "v1_",
            ["IdEncryption:Keys:0:Key"] = IdEncryptionKey,
            ["IdEncryption:Keys:0:IsDefault"] = bool.TrueString,
        };
}
