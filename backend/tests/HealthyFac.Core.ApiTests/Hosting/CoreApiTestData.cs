using HealthyFac.Shared.Web.CurrentUser.ServiceActors;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Core.ApiTests.Hosting;

public static class CoreApiTestData
{
    public const string QueueNamePrefix = "core-api-tests-";
    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";
    public const string InviteTokenSecret = "dGVzdC1pbnZpdGUtdG9rZW4ta2V5LTAxMjM0NTY3ODk=";
    public const string CompanyName = "Api Test Company";
    public const string CompanyTimeZoneId = "Europe/Kyiv";

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
            [key: "InviteTokens:LifetimeHours"] = "72",
            [key: "InviteTokens:Keys:0:Id"] = "test",
            [key: "InviteTokens:Keys:0:Secret"] = InviteTokenSecret,
            [key: "InviteTokens:Keys:0:IsDefault"] = bool.TrueString,
            [key: "InviteLinks:SpaBaseUrl"] = "https://app.test",
            [key: "Smtp:Host"] = "localhost",
            [key: "Smtp:Port"] = "1025",
            [key: "Smtp:Security"] = "None",
            [key: "Smtp:FromAddress"] = "no-reply@healthyfac.test",
            [key: "Smtp:FromName"] = "HealthyFac Tests",
            [key: "Seeding:IsOptionalDataSeeded"] = bool.FalseString,
            [$"{ServiceActorOptions.SectionName}:AllowedClients:0"] = TestServiceTokenClaims.DefaultClientId,
        };
}
