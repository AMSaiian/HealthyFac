namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// A snapshot of a running chat turn. It holds the full text so far, so the web app replaces the text of the message
/// and never joins pieces.
/// </summary>
public sealed record ChatTurnUpdate
{
    public required Guid ChatId { get; init; }

    public required Guid MessageId { get; init; }

    /// <summary>Rises with each snapshot of one message. The web app drops a snapshot with a lower value.</summary>
    public required int Sequence { get; init; }

    public required string Text { get; init; }

    /// <summary>The value text of the current activity (a tool or a phase), or <see langword="null"/>.</summary>
    public string? Activity { get; init; }
}
