using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// Maps each sort column of the draft grid to its key expression, and holds the default order: pending drafts first
/// (see <see cref="MachineDraftSortColumn.State"/>).
/// </summary>
public static class MachineDraftSortMap
{
    public static readonly FrozenDictionary<MachineDraftSortColumn, Expression<Func<MachineDraftGridItem, dynamic>>>
        Instance =
            new Dictionary<MachineDraftSortColumn, Expression<Func<MachineDraftGridItem, dynamic>>>
            {
                [MachineDraftSortColumn.ProposedAt] = item => item.ProposedAt,
                [MachineDraftSortColumn.State] = item => item.State == DraftState.Pending
                    ? 0
                    : item.State == DraftState.Applied ? 1 : 2,
            }.ToFrozenDictionary();

    public static readonly GridSort<MachineDraftSortColumn> Default = new()
    {
        Column = MachineDraftSortColumn.State,
        Direction = SortDirection.Ascending,
    };
}
