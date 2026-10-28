using HealthyFac.Testing.Authentication;

namespace HealthyFac.Core.ApiTests.Hosting;

public static class CoreApiTestData
{
    public const string QueueNamePrefix = "core-api-tests-";
    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";
    public const string InviteTokenSecret = "dGVzdC1pbnZpdGUtdG9rZW4ta2V5LTAxMjM0NTY3ODk=";
    public const string CompanyName = "Api Test Company";
    public const string DepartmentName = "Api Test Department";
    public const string DepartmentTimeZoneId = "Europe/Kyiv";

    public static IReadOnlyDictionary<string, string?> CreateSettings(
        string databaseConnection,
        string brokerConnection
    )
        => new Dictionary<string, string?>(TestTokenFactory.JwtSettings)
        {
            ["ConnectionStrings:Database"] = databaseConnection,
            ["ConnectionStrings:RabbitMq"] = brokerConnection,
            ["Messaging:QueueName"] = $"{QueueNamePrefix}{Guid.NewGuid():N}",
            ["IdEncryption:Keys:0:Prefix"] = "v1.",
            ["IdEncryption:Keys:0:Key"] = IdEncryptionKey,
            ["IdEncryption:Keys:0:Default"] = bool.TrueString,
            ["InviteTokens:LifetimeHours"] = "72",
            ["InviteTokens:Keys:0:Id"] = "test",
            ["InviteTokens:Keys:0:Secret"] = InviteTokenSecret,
            ["InviteTokens:Keys:0:IsDefault"] = bool.TrueString,
            ["InviteLinks:SpaBaseUrl"] = "https://app.test",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "1025",
            ["Smtp:Security"] = "None",
            ["Smtp:FromAddress"] = "no-reply@healthyfac.test",
            ["Smtp:FromName"] = "HealthyFac Tests",
            ["Seeding:SeedOptionalData"] = bool.FalseString,
        };
}
