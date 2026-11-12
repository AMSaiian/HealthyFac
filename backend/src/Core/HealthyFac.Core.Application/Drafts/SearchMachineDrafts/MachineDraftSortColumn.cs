using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// The sortable columns of the draft grid of a machine.
/// </summary>
public sealed class MachineDraftSortColumn : Enumeration<MachineDraftSortColumn>
{
    public static readonly MachineDraftSortColumn ProposedAt = new(value: "proposedAt");

    /// <summary>Sorts the pending drafts first, then the applied, then the dismissed.</summary>
    public static readonly MachineDraftSortColumn State = new(value: "state");

    private MachineDraftSortColumn(string value)
        : base(value)
    {
    }
}
