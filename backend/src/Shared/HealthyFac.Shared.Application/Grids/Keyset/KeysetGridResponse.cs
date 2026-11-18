namespace HealthyFac.Shared.Application.Grids.Keyset;

public sealed record KeysetGridResponse<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    /// <summary>The number of rows that match the filter, before the page is cut.</summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// The position to send as <c>after</c> for the next page, or <see langword="null"/> when no row follows.
    /// </summary>
    public KeysetPosition? NextPosition { get; init; }
}
