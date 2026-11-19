using System.Text.Json.Serialization;

namespace HealthyFac.Insights.Application.Agents.Triage;

/// <summary>
/// The structured answer of the triage model: the verdict and the reason. The model sees the response schema of this
/// type, so <see cref="TriageChoice"/> lists the 3 verdicts as JSON strings.
/// </summary>
public sealed record TriageVerdict
{
    [JsonConverter(typeof(JsonStringEnumConverter<TriageChoice>))]
    public required TriageChoice Verdict { get; init; }

    public required string Reason { get; init; }
}

/// <summary>
/// The verdicts that the model may give. The values map to the domain <c>TriageVerdictKind</c> by their camelCase
/// name.
/// </summary>
public enum TriageChoice
{
    Dismiss,
    Watch,
    Escalate,
}
