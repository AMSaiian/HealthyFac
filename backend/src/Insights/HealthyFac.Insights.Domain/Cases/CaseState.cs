using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Cases;

/// <summary>
/// Defines the states of a maintenance case. The camelCase values reach the API and the database, so never rename one
/// in place.
/// </summary>
public sealed class CaseState : Enumeration<CaseState>
{
    public static readonly CaseState Open = new(value: "open");
    public static readonly CaseState Resolved = new(value: "resolved");
    public static readonly CaseState Closed = new(value: "closed");

    private CaseState(string value)
        : base(value)
    {
    }
}
