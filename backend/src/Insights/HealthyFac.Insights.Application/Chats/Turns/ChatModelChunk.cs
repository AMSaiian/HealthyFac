namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// One piece of the streamed answer of the chat model.
/// </summary>
public sealed record ChatModelChunk
{
    /// <summary>The new text of the piece. It can be empty when the piece only carries a count or an ending.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Tells that the model stopped because it reached the output token limit.</summary>
    public bool IsLengthLimited { get; init; }

    public int InputTokens { get; init; }

    public int OutputTokens { get; init; }
}
