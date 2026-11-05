using HealthyFac.Testing.Authentication;

namespace HealthyFac.Gateway.IntegrationTests.Revocation.Data;

public static class RevocationTestData
{
    public const string CoreRoute = "/api/core/v1/machines";

    public static readonly TimeSpan RevocationTimeToLive = TimeSpan.FromMinutes(minutes: 6);
    public static readonly TimeSpan TokenAge = TimeSpan.FromMinutes(minutes: 10);
    public static readonly TimeSpan RevocationAge = TimeSpan.FromMinutes(minutes: 5);
    public static readonly TimeSpan TokenYoungAge = TimeSpan.FromMinutes(minutes: 1);

    private static readonly TimeSpan LongLifetime = TimeSpan.FromHours(hours: 1);

    public static TestTokenClaims CreateClaims(Guid subject, TimeSpan tokenAge) =>
        new()
        {
            Subject = subject,
            IssuedAt = DateTimeOffset.UtcNow - tokenAge,
            Lifetime = LongLifetime,
        };
}
