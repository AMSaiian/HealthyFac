namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Defines the exceptions of the real-time notifier.
/// </summary>
public static class RealtimeErrors
{
    public static InvalidOperationException KindNotUserTargeted(RealtimeNotificationKind kind) =>
        new($"The kind '{kind.Value}' goes to a group, so add it with AddToGroup.");

    public static InvalidOperationException KindNotGrouped(RealtimeNotificationKind kind) =>
        new($"The kind '{kind.Value}' goes to one user, so add it with AddToUser.");

    public static InvalidOperationException NotRegistered() =>
        new(message: "This service has no real-time notifier. Call AddRealtimeNotifier before you add an event.");
}
