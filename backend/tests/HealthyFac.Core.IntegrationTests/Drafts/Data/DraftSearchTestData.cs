using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

public static class DraftSearchTestData
{
    public const int PageLimit = 2;
    public const int NoFilterPageLimit = 25;
    public const int NoFilterDraftCount = 3;
    public const int PendingCount = 3;
    public const int DismissedCount = 2;
    public const int DraftCount = PendingCount + DismissedCount;

    /// <summary>The case name, the sort, and the expected order of the stored drafts as draft IDs.</summary>
    public static TheoryData<
            string,
            GridSort<MachineDraftSortColumn>,
            Func<IReadOnlyList<MachineDraft>, IEnumerable<Guid>>>
        Sorts => new()
    {
        {
            "state ascending: pending first, equal ranks by ID",
            MachineDraftSortMap.Default,
            drafts => drafts.OrderBy(draft => draft.State == DraftState.Pending ? 0 : 3)
                .ThenBy(draft => draft.Id)
                .Select(draft => draft.Id)
        },
        {
            "proposed time descending, equal times by ID",
            new GridSort<MachineDraftSortColumn>
            {
                Column = MachineDraftSortColumn.ProposedAt,
                Direction = SortDirection.Descending,
            },
            drafts => drafts.OrderByDescending(draft => draft.CreatedAt)
                .ThenByDescending(draft => draft.Id)
                .Select(draft => draft.Id)
        },
    };
}
