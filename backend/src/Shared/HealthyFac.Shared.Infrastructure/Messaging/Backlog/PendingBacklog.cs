namespace HealthyFac.Shared.Infrastructure.Messaging.Backlog;

/// <summary>
/// The pending rows of one messaging table at the time of a sample.
/// </summary>
internal sealed record PendingBacklog
{
    public static readonly PendingBacklog Empty = new() { Count = 0, OldestOn = null };

    public required int Count { get; init; }

    /// <summary>The UTC time of the oldest pending row, or <see langword="null"/> when no row is pending.</summary>
    public required DateTime? OldestOn { get; init; }
}
