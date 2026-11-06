using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.IntegrationTests.Grids.Data;

public sealed class GridSortColumn : Enumeration<GridSortColumn>
{
    public static readonly GridSortColumn Text = new(value: "text");
    public static readonly GridSortColumn Number = new(value: "number");
    public static readonly GridSortColumn Amount = new(value: "amount");
    public static readonly GridSortColumn At = new(value: "at");
    public static readonly GridSortColumn Key = new(value: "key");

    private GridSortColumn(string value)
        : base(value)
    {
    }
}

public sealed record GridTestFilter : IFilter<GridTestEntity>
{
    public TextFilter? Text { get; init; }

    public DateRangeFilter? At { get; init; }

    public DateRangeFilter? OptionalAt { get; init; }

    public Expression<Func<GridTestEntity, bool>>? ToPredicate() =>
        Predicates.From(Text?.ToPredicate<GridTestEntity>(row => row.Text))
            .And(At?.ToPredicate<GridTestEntity>(row => row.At))
            .And(OptionalAt?.ToPredicate<GridTestEntity>(row => row.OptionalAt));
}

public static class GridTestSortMap
{
    public static readonly FrozenDictionary<GridSortColumn, Expression<Func<GridTestEntity, dynamic>>> Instance =
        new Dictionary<GridSortColumn, Expression<Func<GridTestEntity, dynamic>>>
        {
            [GridSortColumn.Text] = row => row.Text,
            [GridSortColumn.Number] = row => row.Number,
            [GridSortColumn.Amount] = row => row.Amount,
            [GridSortColumn.At] = row => row.At,
            [GridSortColumn.Key] = row => row.Key,
        }.ToFrozenDictionary();
}
