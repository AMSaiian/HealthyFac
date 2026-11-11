using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

public sealed class MetricUnitCacheOptions
{
    public const string SectionName = "MetricUnitCache";

    private const int MillisecondsPerSecond = 1000;

    /// <summary>The time to live of the cached catalog in seconds, before the jitter.</summary>
    public int TimeToLiveSeconds { get; init; } = 86400;

    /// <summary>The longest random time in seconds that is added to the time to live of each entry.</summary>
    public int JitterSeconds { get; init; } = 3600;

    /// <summary>
    /// The time to live of the fetch lock in seconds, and the longest wait of an instance for the holder.
    /// </summary>
    public int LockSeconds { get; init; } = 30;

    /// <summary>The time in milliseconds between two reads of a waiting instance.</summary>
    public int PollMilliseconds { get; init; } = 100;

    internal TimeSpan LockTimeout => TimeSpan.FromSeconds(LockSeconds);

    internal bool IsPollBelowLock => PollMilliseconds < LockSeconds * MillisecondsPerSecond;
}

public sealed class MetricUnitCacheOptionsValidator : AbstractValidator<MetricUnitCacheOptions>
{
    public MetricUnitCacheOptionsValidator()
    {
        RuleFor(options => options.TimeToLiveSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(MetricUnitCacheMessages.TimeToLiveInvalid);
        RuleFor(options => options.JitterSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(MetricUnitCacheMessages.JitterInvalid);
        RuleFor(options => options.LockSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(MetricUnitCacheMessages.LockInvalid);
        RuleFor(options => options.PollMilliseconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(MetricUnitCacheMessages.PollInvalid);
        RuleFor(options => options)
            .Must(options => options.IsPollBelowLock)
            .WithMessage(MetricUnitCacheMessages.PollAboveLock);
    }
}
