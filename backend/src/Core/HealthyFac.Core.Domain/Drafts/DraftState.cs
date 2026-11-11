using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// Defines the states of a draft. The camelCase values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class DraftState : Enumeration<DraftState>
{
    /// <summary>The draft waits for a user.</summary>
    public static readonly DraftState Pending = new(value: "pending");

    /// <summary>A user applied the draft.</summary>
    public static readonly DraftState Applied = new(value: "applied");

    /// <summary>A user dismissed the draft, or its target machine was deleted.</summary>
    public static readonly DraftState Dismissed = new(value: "dismissed");

    private DraftState(string value)
        : base(value)
    {
    }
}
