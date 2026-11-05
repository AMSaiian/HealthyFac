namespace HealthyFac.Shared.Infrastructure.Caching;

public static class RedisConstants
{
    public const int MinTimeoutMilliseconds = 100;
    public const int MaxTimeoutMilliseconds = 30000;

    /// <summary>
    /// The settings of the shared Redis pipeline and its circuit breaker.
    /// </summary>
    public static class Resilience
    {
        /// <summary>The key of the shared Redis pipeline in the Polly registry.</summary>
        public const string PipelineKey = "redis";

        /// <summary>The name of the health check that reports the state of the breaker.</summary>
        public const string HealthCheckName = "redis-breaker";

        /// <summary>
        /// The limit of the breaker health check. It covers the 1 ping that the check sends while the breaker is
        /// not closed.
        /// </summary>
        public static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(seconds: 2);

        /// <summary>The share of failed calls in the sampling period that opens the breaker.</summary>
        public const double FailureRatio = 0.5;

        /// <summary>The least number of calls in the sampling period before the breaker can open.</summary>
        public const int MinimumThroughput = 2;

        /// <summary>The period in which the breaker counts the calls and the failures.</summary>
        public static readonly TimeSpan SamplingDuration = TimeSpan.FromSeconds(seconds: 30);

        /// <summary>How long the open breaker fails every call at once before it lets 1 probe through.</summary>
        public static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(seconds: 30);
    }
}
