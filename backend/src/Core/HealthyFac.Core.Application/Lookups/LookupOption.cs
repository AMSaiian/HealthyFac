namespace HealthyFac.Core.Application.Lookups;

/// <summary>An option of a picker or a filter: the ID of a row and the name that the picker shows.</summary>
public record LookupOption
{
    public required Guid Id { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>
    /// Tells that the row is soft deleted. A picker never lists a deleted row, so it is always false there.
    /// </summary>
    public required bool IsDeleted { get; init; }
}

public record LookupResponse
{
    public required IReadOnlyList<LookupOption> Items { get; init; }
}
