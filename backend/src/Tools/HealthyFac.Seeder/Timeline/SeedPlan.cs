using HealthyFac.Seeder.SeedFiles;

namespace HealthyFac.Seeder.Timeline;

/// <summary>Collects the events of all parts, in the order of the parts and of their files.</summary>
public static class SeedPlan
{
    /// <summary>Reads the events of every part.</summary>
    /// <param name="parts">The parts.</param>
    /// <param name="folder">The seed folder.</param>
    /// <returns>The events with their parts, in file order.</returns>
    public static IReadOnlyList<PlannedEvent> Load(IReadOnlyList<ISeedPart> parts, SeedFolder folder) =>
    [
        .. parts
            .SelectMany(part => part.ReadEvents(folder)
                .Select(seedEvent => new PlannedEvent
                {
                    Part = part,
                    Event = seedEvent,
                })),
    ];
}
