namespace HealthyFac.Seeder.Timeline;

/// <summary>A seed event with the part that runs it.</summary>
public sealed class PlannedEvent
{
    public required ISeedPart Part { get; init; }

    public required SeedEvent Event { get; init; }
}
