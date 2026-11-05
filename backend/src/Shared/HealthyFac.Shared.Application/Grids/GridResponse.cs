namespace HealthyFac.Shared.Application.Grids;

public sealed record GridResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    /// <summary>The number of rows that match the filter, before the page is cut.</summary>
    public required int TotalCount { get; init; }
}
