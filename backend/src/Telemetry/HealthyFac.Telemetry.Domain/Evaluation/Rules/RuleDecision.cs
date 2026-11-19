using HealthyFac.Telemetry.Domain.SignalEpisodes;

namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// What a rule decided for one sample.
/// </summary>
public enum RuleDecisionKind
{
    None,
    Open,
    Escalate,
    Resolve,
}

/// <summary>
/// The decision of a rule for one sample, with the severity of an open or an escalate and the numbers of the sample.
/// </summary>
public sealed record RuleDecision
{
    public static RuleDecision None { get; } = new() { Kind = RuleDecisionKind.None, Snapshot = SignalSnapshot.Empty };

    public required RuleDecisionKind Kind { get; init; }

    /// <summary>The severity of an open or an escalate, otherwise <see langword="null"/>.</summary>
    public SignalSeverity? Severity { get; init; }

    public required SignalSnapshot Snapshot { get; init; }
}
