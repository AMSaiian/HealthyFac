namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// A scheduled work order that a rejected shift change would leave outside the shift. The rejection lists it.
/// </summary>
public record AffectedWorkOrder
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required DateTime PlannedStartLocal { get; init; }
}
