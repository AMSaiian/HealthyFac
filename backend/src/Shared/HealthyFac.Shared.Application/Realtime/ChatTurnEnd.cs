namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// The end of a chat turn. The push goes out after the final save.
/// </summary>
public sealed record ChatTurnEnd
{
    public required Guid ChatId { get; init; }

    public required Guid MessageId { get; init; }

    /// <summary>The value text of the final state of the message.</summary>
    public required string State { get; init; }
}
