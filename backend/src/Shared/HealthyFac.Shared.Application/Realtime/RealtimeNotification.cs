namespace HealthyFac.Shared.Application.Realtime;

public sealed record RealtimeNotification
{
    public required Guid NotificationId { get; init; }

    public required RealtimeNotificationKind Kind { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The <c>sub</c> of the user who caused the change, or <see langword="null"/> for the system.</summary>
    public Guid? ActorSubject { get; init; }

    /// <summary>
    /// Declared as <see cref="object"/>, so the JSON protocol writes the runtime type of the kind data.
    /// </summary>
    public required object Data { get; init; }
}
