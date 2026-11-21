namespace HealthyFac.Shared.Infrastructure.BackgroundQueues;

/// <summary>
/// Defines the exceptions of the background queues.
/// </summary>
public static class BackgroundQueueErrors
{
    public static InvalidOperationException HandlerCount(Type itemType, int count) =>
        new(
            $"The background queue of '{itemType.FullName}' needs exactly 1 handler, but {count} are registered. "
            + "Register one IBackgroundItemHandler for the item type."
        );

    public static ArgumentOutOfRangeException LimitInvalid(int limit) =>
        new(nameof(limit), limit, message: "The limit of a bounded background queue must be above 0.");
}
