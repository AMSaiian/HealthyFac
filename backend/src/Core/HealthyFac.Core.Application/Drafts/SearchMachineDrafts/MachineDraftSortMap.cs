using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// Maps each sort column of the draft grid to its key expression, and holds the default order: the newest proposal
/// first.
/// </summary>
public static class MachineDraftSortMap
{
    public static readonly FrozenDictionary<MachineDraftSortColumn, Expression<Func<MachineDraftGridItem, dynamic>>>
        Instance =
            new Dictionary<MachineDraftSortColumn, Expression<Func<MachineDraftGridItem, dynamic>>>
            {
                [MachineDraftSortColumn.ProposedAt] = item => item.ProposedAt,
            }.ToFrozenDictionary();

    public static readonly GridSort<MachineDraftSortColumn> Default = new()
    {
        Column = MachineDraftSortColumn.ProposedAt,
        Direction = SortDirection.Descending,
    };
}
