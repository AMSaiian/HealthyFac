namespace HealthyFac.Insights.Infrastructure.Caching;

public static class CacheMessages
{
    public const string TimeToLiveInvalid = "TimeToLiveSeconds must be above 0.";
    public const string JitterInvalid = "JitterSeconds must be at least 0.";
    public const string LockInvalid = "LockSeconds must be above 0.";
    public const string PollInvalid = "PollMilliseconds must be above 0.";
    public const string PollAboveLock = "PollMilliseconds must be below the lock time.";
}
