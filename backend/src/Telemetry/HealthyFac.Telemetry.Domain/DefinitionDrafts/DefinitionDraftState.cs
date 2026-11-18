using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// Defines the states of a metric or status definition draft. The camelCase values reach the API and the database,
/// so never rename one in place. The order of the ranks of the draft grid is <c>pending</c>, <c>awaitingTarget</c>,
/// <c>applied</c>, <c>dismissed</c>.
/// </summary>
public sealed class DefinitionDraftState : Enumeration<DefinitionDraftState>
{
    /// <summary>The draft waits for the machine check of its proposal. A user cannot apply it yet.</summary>
    public static readonly DefinitionDraftState AwaitingTarget = new(value: "awaitingTarget");

    /// <summary>The draft waits for a user.</summary>
    public static readonly DefinitionDraftState Pending = new(value: "pending");

    /// <summary>A user applied the draft.</summary>
    public static readonly DefinitionDraftState Applied = new(value: "applied");

    /// <summary>A user dismissed the draft, or its target machine was deleted or never created.</summary>
    public static readonly DefinitionDraftState Dismissed = new(value: "dismissed");

    private DefinitionDraftState(string value)
        : base(value)
    {
    }
}
