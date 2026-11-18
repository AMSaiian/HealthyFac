using System.Collections.Frozen;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;

/// <summary>
/// Maps each sort column of the definition draft grid to its keyset sort key, and holds the default order: pending
/// drafts first (see <see cref="DefinitionDraftSortColumn.State"/>). The state rank is the one of Core too (pending 0,
/// awaitingTarget 1, applied 2, dismissed 3), so the Gateway merges both services in one order.
/// </summary>
public static class DefinitionDraftSortMap
{
    public static readonly FrozenDictionary<DefinitionDraftSortColumn, IKeysetSortKey<DefinitionDraftGridItem>>
        Instance = new Dictionary<DefinitionDraftSortColumn, IKeysetSortKey<DefinitionDraftGridItem>>
        {
            [DefinitionDraftSortColumn.ProposedAt] =
                KeysetSortKey.For((DefinitionDraftGridItem item) => item.ProposedAt),
            [DefinitionDraftSortColumn.State] = KeysetSortKey.For((DefinitionDraftGridItem item) =>
                item.State == DefinitionDraftState.Pending
                    ? 0
                    : item.State == DefinitionDraftState.AwaitingTarget
                        ? 1
                        : item.State == DefinitionDraftState.Applied
                            ? 2
                            : 3),
        }.ToFrozenDictionary();

    public static readonly GridSort<DefinitionDraftSortColumn> Default = new()
    {
        Column = DefinitionDraftSortColumn.State,
        Direction = SortDirection.Ascending,
    };
}
