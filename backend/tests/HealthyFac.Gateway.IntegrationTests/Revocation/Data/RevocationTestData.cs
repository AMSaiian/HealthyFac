using HealthyFac.Testing.Authentication;

namespace HealthyFac.Gateway.IntegrationTests.Revocation.Data;

public static class RevocationTestData
{
    public const string CoreRoute = "/api/core/v1/machines";
    public const string RedisCircuitBreakerCategory = "HealthyFac.Shared.Infrastructure.Caching.RedisCircuitBreaker";

    public static readonly TimeSpan RevocationTimeToLive = TimeSpan.FromMinutes(6);
    public static readonly TimeSpan TokenAge = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan RevocationAge = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan TokenYoungAge = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan LongLifetime = TimeSpan.FromHours(1);

    public static TestTokenClaims CreateClaims(Guid subject, TimeSpan tokenAge) =>
        new()
        {
            Subject = subject,
            IssuedAt = DateTimeOffset.UtcNow - tokenAge,
            Lifetime = LongLifetime,
        };
}
