namespace HealthyFac.Insights.Infrastructure.MetricUnits;

public static class MetricUnitCacheMessages
{
    public const string TimeToLiveInvalid = "MetricUnitCache:TimeToLiveSeconds must be above 0.";
    public const string JitterInvalid = "MetricUnitCache:JitterSeconds must be at least 0.";
    public const string LockInvalid = "MetricUnitCache:LockSeconds must be above 0.";
    public const string PollInvalid = "MetricUnitCache:PollMilliseconds must be above 0.";
    public const string PollAboveLock = "MetricUnitCache:PollMilliseconds must be below the lock time.";
}
