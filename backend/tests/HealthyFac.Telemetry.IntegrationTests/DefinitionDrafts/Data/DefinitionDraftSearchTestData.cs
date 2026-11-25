using HealthyFac.Shared.Application.Grids;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

public static class DefinitionDraftSearchTestData
{
    public const int PageLimit = 2;
    public const int ChatPageLimit = 50;

    /// <summary>Two pending metrics, a pending status, an awaiting status, and an applied metric.</summary>
    public const int DraftCount = 5;

    /// <summary>The case name, the sort, and the expected order of the stored drafts as draft IDs.</summary>
    public static TheoryData<
            string,
            GridSort<DefinitionDraftSortColumn>,
            Func<IReadOnlyList<MetricDefinitionDraft>, IReadOnlyList<StatusDefinitionDraft>, IEnumerable<Guid>>>
        Sorts => new()
    {
        {
            "state ascending: pending, awaiting, applied; equal ranks by ID",
            DefinitionDraftSortMap.Default,
            (metrics, statuses) => metrics.Select(draft => (draft.Id, draft.State))
                .Concat(statuses.Select(draft => (draft.Id, draft.State)))
                .OrderBy(row => Rank(row.State))
                .ThenBy(row => row.Id)
                .Select(row => row.Id)
        },
        {
            "proposed time descending, equal times by ID",
            new GridSort<DefinitionDraftSortColumn>
            {
                Column = DefinitionDraftSortColumn.ProposedAt,
                Direction = SortDirection.Descending,
            },
            (metrics, statuses) => metrics.Select(draft => (draft.Id, draft.CreatedAt))
                .Concat(statuses.Select(draft => (draft.Id, draft.CreatedAt)))
                .OrderByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .Select(row => row.Id)
        },
    };

    private static int Rank(DefinitionDraftState state) =>
        state == DefinitionDraftState.Pending ? 0
        : state == DefinitionDraftState.AwaitingTarget ? 1
        : state == DefinitionDraftState.Applied ? 2 : 3;
}
