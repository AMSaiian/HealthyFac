using StackExchange.Redis;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public static class FallbackRateLimiterTestData
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan PrimaryIdleDuration = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan LocalIdleDuration = TimeSpan.FromSeconds(5);

    public static FakePrimaryRateLimiter CreateFailingPrimary() =>
        new(() => throw new RedisException("Redis is down."));

    public static FakePrimaryRateLimiter CreateGrantingPrimary() =>
        new(() => new FakeRateLimitLease(isAcquired: true));
}
