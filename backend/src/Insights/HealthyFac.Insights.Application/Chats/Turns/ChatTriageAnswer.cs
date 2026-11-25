namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// The answer of the chat triage model with the tokens of the call.
/// </summary>
public sealed record ChatTriageAnswer
{
    public required ChatTriageVerdict Verdict { get; init; }

    public required int InputTokens { get; init; }

    public required int OutputTokens { get; init; }
}
