using System.Collections.Frozen;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// Maps each sort column of the draft grid to its keyset sort key, and holds the default order: pending drafts first
/// (see <see cref="MachineDraftSortColumn.State"/>). The state rank is the one of Telemetry (pending 0, applied 2,
/// dismissed 3; Core has no <c>awaitingTarget</c>), so the Gateway merges both services in one order.
/// </summary>
public static class MachineDraftSortMap
{
    public static readonly FrozenDictionary<MachineDraftSortColumn, IKeysetSortKey<MachineDraftGridItem>> Instance =
        new Dictionary<MachineDraftSortColumn, IKeysetSortKey<MachineDraftGridItem>>
        {
            [MachineDraftSortColumn.ProposedAt] = KeysetSortKey.For((MachineDraftGridItem item) => item.ProposedAt),
            [MachineDraftSortColumn.State] = KeysetSortKey.For((MachineDraftGridItem item) =>
                item.State == DraftState.Pending
                    ? 0
                    : item.State == DraftState.Applied
                        ? 2
                        : 3),
        }.ToFrozenDictionary();

    public static readonly GridSort<MachineDraftSortColumn> Default = new()
    {
        Column = MachineDraftSortColumn.State,
        Direction = SortDirection.Ascending,
    };
}
