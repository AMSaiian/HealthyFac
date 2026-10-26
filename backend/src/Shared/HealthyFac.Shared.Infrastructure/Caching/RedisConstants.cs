namespace HealthyFac.Shared.Infrastructure.Caching;

public static class RedisConstants
{
    /// <summary>How long the circuit breaker skips Redis after a failure.</summary>
    public static readonly TimeSpan OpenPeriod = TimeSpan.FromSeconds(30);

    /// <summary>How long one probe may run before another caller starts a new probe.</summary>
    public static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    public const int MinTimeoutMilliseconds = 100;
    public const int MaxTimeoutMilliseconds = 30000;
}
