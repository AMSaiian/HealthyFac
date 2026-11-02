using System.Threading.RateLimiting;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

/// <summary>
/// A limiter that stands in for the local limiter. It grants every acquire. A real fixed window limiter reports
/// <c>null</c> idle time after it grants a permit, so it cannot show the idle rule of the fallback limiter.
/// A test sets <see cref="ReportedIdleDuration"/>, because <see cref="IdleDuration"/> has no setter to override.
/// </summary>
public sealed class FakeLocalRateLimiter : ReplenishingRateLimiter
{
    public TimeSpan? ReportedIdleDuration { get; set; }

    public override TimeSpan? IdleDuration => ReportedIdleDuration;

    public override bool IsAutoReplenishing => false;

    public override TimeSpan ReplenishmentPeriod => TimeSpan.FromMinutes(1);

    public override RateLimiterStatistics? GetStatistics() => null;

    public override bool TryReplenish() => false;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) => new FakeRateLimitLease(isAcquired: true);

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult<RateLimitLease>(new FakeRateLimitLease(isAcquired: true));
}
