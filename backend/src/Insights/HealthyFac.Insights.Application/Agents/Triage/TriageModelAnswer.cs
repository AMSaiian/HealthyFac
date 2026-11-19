namespace HealthyFac.Insights.Application.Agents.Triage;

/// <summary>
/// The answer of the triage model with the tokens of the call.
/// </summary>
public sealed record TriageModelAnswer
{
    public required TriageVerdict Verdict { get; init; }

    public required int InputTokens { get; init; }

    public required int OutputTokens { get; init; }
}
