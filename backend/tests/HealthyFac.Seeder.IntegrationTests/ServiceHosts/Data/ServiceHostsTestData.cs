using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.IntegrationTests.ServiceHosts.Data;

public static class ServiceHostsTestData
{
    public const int RoleCount = 2;
    public const string ClientsPathFormat = "admin/realms/{0}/clients";
    public const string AuthClientId = "healthyfac-auth";
    public const string TokenLifespanAttribute = "access.token.lifespan";
    public const int ShortLifespanSeconds = 2;
    public static readonly TimeSpan ClockJump = TimeSpan.FromDays(days: 30);
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMilliseconds(milliseconds: 100);
    public const string BlobUrlKey = "Services:document:FileStorage:PublicBlobBaseUrl";
    public const string OverrideBlobUrl = "https://override.test/blobs";
    public const string RequiredKey = "Services:core:InviteTokens:Keys:0:Secret";
    public const string RequiredKeyMessage = "*core*Secret*";

    public static string? ReadDevelopmentSetting(string service, string section, string key)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, path2: "services", service, path4: "appsettings.Development.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement.GetProperty(section).GetProperty(key).GetString();
    }

    public static readonly Guid CompanyId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000c001");

    public static readonly ActorContext Actor = new()
    {
        Kind = ActorKind.User,
        UserId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000a001"),
        CompanyId = CompanyId,
    };
}
