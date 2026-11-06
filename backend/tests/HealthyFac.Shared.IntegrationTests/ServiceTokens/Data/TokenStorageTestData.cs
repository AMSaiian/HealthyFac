namespace HealthyFac.Shared.IntegrationTests.ServiceTokens.Data;

public static class TokenStorageTestData
{
    public const string StoredValue = """{"Token":"token-1","RefreshAt":"2026-01-01T00:04:00+00:00"}""";
    public const string FirstOwner = "owner-a";
    public const string SecondOwner = "owner-b";

    public static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(minutes: 5);

    public static readonly TimeSpan LockTimeToLive = TimeSpan.FromSeconds(seconds: 10);

    public static string NewKey() => $"test:token:{Guid.NewGuid():N}";
}
