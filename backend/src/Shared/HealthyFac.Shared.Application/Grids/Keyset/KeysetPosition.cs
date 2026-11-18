using System.Text.Json;

namespace HealthyFac.Shared.Application.Grids.Keyset;

/// <summary>
/// The position of one row in a keyset grid: the sort value of the row and its ID. The client sends the position of
/// the last row of a page back as <c>after</c> to read the next page.
/// </summary>
public sealed record KeysetPosition
{
    public required JsonElement SortValue { get; init; }

    public required Guid Id { get; init; }
}
