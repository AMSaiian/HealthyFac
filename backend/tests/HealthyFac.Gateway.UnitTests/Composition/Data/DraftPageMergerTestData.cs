using HealthyFac.Gateway.Web.Composition;
using HealthyFac.Gateway.Web.Composition.Drafts.Search;

namespace HealthyFac.Gateway.UnitTests.Composition.Data;

public static class DraftPageMergerTestData
{
    public const int PageLimit = 2;

    /// <summary>
    /// The case name, the sort column, the direction, the rows of Core, the rows of Telemetry, and the expected IDs
    /// of the merged grid. Equal sort values exist inside and across the sources, so the tie rules matter.
    /// </summary>
    public static TheoryData<
        string,
        string,
        bool,
        DraftPagingSimulator,
        DraftPagingSimulator,
        string[]> Grids => new()
    {
        {
            "state ascending with ties",
            DraftSortComparers.State,
            false,
            StateRows(("0", "c1"), ("0", "c2"), ("1", "c3"), ("3", "c4")),
            StateRows(("0", "t1"), ("1", "t2"), ("1", "t3"), ("2", "t4")),
            ["c1", "c2", "t1", "c3", "t2", "t3", "t4", "c4"]
        },
        {
            "state descending with ties",
            DraftSortComparers.State,
            true,
            StateRows(("0", "c1"), ("0", "c2"), ("1", "c3"), ("3", "c4")),
            StateRows(("0", "t1"), ("1", "t2"), ("1", "t3"), ("2", "t4")),
            ["c4", "t4", "c3", "t2", "t3", "c1", "c2", "t1"]
        },
        {
            "proposed time ascending with an equal time across the sources",
            DraftSortComparers.ProposedAt,
            false,
            TimeRows(("2026-01-01T10:00:00Z", "c1"), ("2026-01-01T12:00:00Z", "c2"), ("2026-01-02T08:00:00Z", "c3")),
            TimeRows(("2026-01-01T09:00:00Z", "t1"), ("2026-01-01T12:00:00Z", "t2"), ("2026-01-03T08:00:00Z", "t3")),
            ["t1", "c1", "c2", "t2", "c3", "t3"]
        },
        {
            "proposed time descending with an equal time across the sources",
            DraftSortComparers.ProposedAt,
            true,
            TimeRows(("2026-01-01T10:00:00Z", "c1"), ("2026-01-01T12:00:00Z", "c2"), ("2026-01-02T08:00:00Z", "c3")),
            TimeRows(("2026-01-01T09:00:00Z", "t1"), ("2026-01-01T12:00:00Z", "t2"), ("2026-01-03T08:00:00Z", "t3")),
            ["t3", "c3", "c2", "t2", "c1", "t1"]
        },
        {
            "one source without rows",
            DraftSortComparers.State,
            false,
            StateRows(("0", "c1"), ("1", "c2"), ("2", "c3")),
            StateRows(),
            ["c1", "c2", "c3"]
        },
    };

    public static TheoryData<string, string, string> InvalidRows => new()
    {
        {
            "sort value of another kind",
            """{"id":"c1","position":{"sortValue":"pending","id":"c1"}}""",
            CompositionErrors.SortValueMismatch
        },
        { "row without a position", """{"id":"c1"}""", CompositionErrors.RowPositionMissing },
    };

    private static DraftPagingSimulator StateRows(params (string Value, string Id)[] rows) => new(rows);

    private static DraftPagingSimulator TimeRows(params (string Value, string Id)[] rows) =>
        new([.. rows.Select(row => ($"\"{row.Value}\"", row.Id))]);
}
