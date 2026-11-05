using StackExchange.Redis;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public static class FallbackRateLimiterTestData
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(minutes: 1);
    public static readonly TimeSpan PrimaryIdleDuration = TimeSpan.FromSeconds(seconds: 3);
    public static readonly TimeSpan LocalIdleDuration = TimeSpan.FromSeconds(seconds: 5);

    public static FakePrimaryRateLimiter CreateFailingPrimary() =>
        new(() => throw new RedisException(message: "Redis is down."));

    public static FakePrimaryRateLimiter CreateGrantingPrimary() =>
        new(() => new FakeRateLimitLease(isAcquired: true));
}
