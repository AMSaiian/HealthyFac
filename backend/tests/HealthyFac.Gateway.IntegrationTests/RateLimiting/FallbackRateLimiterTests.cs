using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;
using HealthyFac.Gateway.Web.RateLimiting;
using HealthyFac.Shared.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting;

public sealed class FallbackRateLimiterTests
{
    private readonly RedisCircuitBreaker _breaker =
        new(new FakeTimeProvider(), NullLogger<RedisCircuitBreaker>.Instance);

    [Fact]
    public async Task Acquire_WhenPrimaryThrowsRedisException_UsesLocalLimiterAndOpensBreaker()
    {
        var primary = FallbackRateLimiterTestData.CreateFailingPrimary();
        await using var limiter = CreateLimiter(primary, new FakeLocalRateLimiter());

        using var lease = await limiter.AcquireAsync();

        var canTryRedis = _breaker.CanTryRedis();

        lease.IsAcquired.Should().BeTrue();
        canTryRedis.Should().BeFalse();
    }

    [Fact]
    public async Task Acquire_WhileBreakerIsOpen_SkipsPrimary()
    {
        var primary = FallbackRateLimiterTestData.CreateFailingPrimary();
        await using var limiter = CreateLimiter(primary, new FakeLocalRateLimiter());
        _breaker.RecordFailure(new TimeoutException());

        using var lease = await limiter.AcquireAsync();

        lease.IsAcquired.Should().BeTrue();
        primary.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Acquire_WhenPrimarySucceeds_ReturnsPrimaryLease()
    {
        var primaryLease = new FakeRateLimitLease(isAcquired: false);
        var primary = new FakePrimaryRateLimiter(() => primaryLease);
        await using var limiter = CreateLimiter(primary, new FakeLocalRateLimiter());

        var lease = await limiter.AcquireAsync();

        var canTryRedis = _breaker.CanTryRedis();

        lease.Should().BeSameAs(primaryLease);
        canTryRedis.Should().BeTrue();
    }

    [Fact]
    public async Task Acquire_WhenPrimarySucceeds_DoesNotCreateLocalLimiter()
    {
        var createLocalCallCount = 0;
        var primary = FallbackRateLimiterTestData.CreateGrantingPrimary();
        await using var limiter = new FallbackRateLimiter(
            primary,
            () =>
            {
                createLocalCallCount++;

                return new FakeLocalRateLimiter();
            },
            FallbackRateLimiterTestData.Window,
            _breaker);

        using var lease = await limiter.AcquireAsync();

        createLocalCallCount.Should().Be(0);
    }

    [Fact]
    public async Task IdleDuration_BeforeFallback_ReturnsPrimaryIdleDuration()
    {
        var primary = FallbackRateLimiterTestData.CreateGrantingPrimary();
        primary.ReportedIdleDuration = FallbackRateLimiterTestData.PrimaryIdleDuration;
        await using var limiter = CreateLimiter(primary, new FakeLocalRateLimiter());

        var idleDuration = limiter.IdleDuration;

        idleDuration.Should().Be(FallbackRateLimiterTestData.PrimaryIdleDuration);
    }

    [Fact]
    public async Task IdleDuration_AfterFallbackWithActivePrimary_ReturnsNull()
    {
        var primary = FallbackRateLimiterTestData.CreateFailingPrimary();
        var local = new FakeLocalRateLimiter();
        await using var limiter = await CreateFallenBackLimiterAsync(primary, local);
        primary.ReportedIdleDuration = null;
        local.ReportedIdleDuration = FallbackRateLimiterTestData.LocalIdleDuration;

        var idleDuration = limiter.IdleDuration;

        idleDuration.Should().BeNull();
    }

    [Fact]
    public async Task IdleDuration_AfterFallbackWithActiveLocal_ReturnsNull()
    {
        var primary = FallbackRateLimiterTestData.CreateFailingPrimary();
        var local = new FakeLocalRateLimiter();
        await using var limiter = await CreateFallenBackLimiterAsync(primary, local);
        primary.ReportedIdleDuration = FallbackRateLimiterTestData.PrimaryIdleDuration;
        local.ReportedIdleDuration = null;

        var idleDuration = limiter.IdleDuration;

        idleDuration.Should().BeNull();
    }

    [Fact]
    public async Task IdleDuration_AfterFallbackWithIdleLimiters_ReturnsSmallerValue()
    {
        var primary = FallbackRateLimiterTestData.CreateFailingPrimary();
        var local = new FakeLocalRateLimiter();
        await using var limiter = await CreateFallenBackLimiterAsync(primary, local);
        primary.ReportedIdleDuration = FallbackRateLimiterTestData.PrimaryIdleDuration;
        local.ReportedIdleDuration = FallbackRateLimiterTestData.LocalIdleDuration;

        var idleDuration = limiter.IdleDuration;

        idleDuration.Should().Be(FallbackRateLimiterTestData.PrimaryIdleDuration);
    }

    private FallbackRateLimiter CreateLimiter(FakePrimaryRateLimiter primary, FakeLocalRateLimiter local) =>
        new(primary, () => local, FallbackRateLimiterTestData.Window, _breaker);

    private async Task<FallbackRateLimiter> CreateFallenBackLimiterAsync(
        FakePrimaryRateLimiter primary,
        FakeLocalRateLimiter local
    )
    {
        var limiter = CreateLimiter(primary, local);
        using var lease = await limiter.AcquireAsync();

        return limiter;
    }
}
