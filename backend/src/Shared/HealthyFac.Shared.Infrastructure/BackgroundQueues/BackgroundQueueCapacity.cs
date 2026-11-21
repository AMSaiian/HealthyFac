namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

/// <summary>
/// The capacity of a background queue. A full bounded queue drops its oldest item.
/// </summary>
public sealed record BackgroundQueueCapacity
{
    public static readonly BackgroundQueueCapacity Unbounded = new();

    public int? Limit { get; private init; }

    public static BackgroundQueueCapacity Bounded(int limit) =>
        limit > 0 ? new BackgroundQueueCapacity { Limit = limit } : throw BackgroundQueueErrors.LimitInvalid(limit);
}
