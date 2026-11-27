namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// One push that the background sender delivers to the hub. It holds plain data only, because it leaves the scope
/// of the request.
/// </summary>
public sealed record RealtimePush
{
    public required RealtimePushTarget Target { get; init; }

    public Guid? UserId { get; init; }

    public string? Group { get; init; }

    public RealtimeNotification? Notification { get; init; }

    public RealtimeHint? Hint { get; init; }

    public ChatTurnUpdate? ChatTurnUpdate { get; init; }

    public ChatTurnEnd? ChatTurnEnd { get; init; }

    public bool IsSessionEnd { get; init; }

    /// <summary>The value of the <c>realtime.target</c> metric tag: never an ID.</summary>
    public required string MetricTarget { get; init; }
}
