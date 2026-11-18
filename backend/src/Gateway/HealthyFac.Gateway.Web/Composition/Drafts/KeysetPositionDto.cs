using System.Text.Json;

namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>
/// The position of one row as a service shows it: the sort value and the encrypted ID. The Gateway keeps the ID as the
/// text that it got, because it never reads it.
/// </summary>
public sealed record KeysetPositionDto
{
    public required JsonElement SortValue { get; init; }

    public required string Id { get; init; }
}
