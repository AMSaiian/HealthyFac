using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Agents.Runs;

/// <summary>
/// Defines the verdicts of the triage of a case. The camelCase values reach the database, so never rename one in place.
/// </summary>
public sealed class TriageVerdictKind : Enumeration<TriageVerdictKind>
{
    public static readonly TriageVerdictKind Dismiss = new(value: "dismiss");
    public static readonly TriageVerdictKind Watch = new(value: "watch");
    public static readonly TriageVerdictKind Escalate = new(value: "escalate");

    private TriageVerdictKind(string value)
        : base(value)
    {
    }
}
