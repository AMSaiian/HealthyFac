using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;

/// <summary>
/// The sortable columns of the definition draft grid.
/// </summary>
public sealed class DefinitionDraftSortColumn : Enumeration<DefinitionDraftSortColumn>
{
    public static readonly DefinitionDraftSortColumn ProposedAt = new(value: "proposedAt");

    /// <summary>
    /// Sorts the pending drafts first, then the drafts that wait for a target, then the applied, then the dismissed.
    /// </summary>
    public static readonly DefinitionDraftSortColumn State = new(value: "state");

    private DefinitionDraftSortColumn(string value)
        : base(value)
    {
    }
}
