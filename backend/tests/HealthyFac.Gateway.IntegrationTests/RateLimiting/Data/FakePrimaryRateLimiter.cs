using System.Threading.RateLimiting;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

/// <summary>
/// A limiter that stands in for the Redis limiter. It counts the calls and runs the supplied behavior.
/// A test sets <see cref="ReportedIdleDuration"/>, because <see cref="IdleDuration"/> has no setter to override.
/// </summary>
public sealed class FakePrimaryRateLimiter(Func<RateLimitLease> acquire) : RateLimiter
{
    private int _callCount;

    public int CallCount => _callCount;

    public TimeSpan? ReportedIdleDuration { get; set; }

    public override TimeSpan? IdleDuration => ReportedIdleDuration;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) => Acquire();

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken
    ) => ValueTask.FromResult(Acquire());

    private RateLimitLease Acquire()
    {
        Interlocked.Increment(ref _callCount);

        return acquire();
    }
}
