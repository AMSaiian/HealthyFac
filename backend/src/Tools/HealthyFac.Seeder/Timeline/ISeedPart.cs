using HealthyFac.Seeder.SeedFiles;

namespace HealthyFac.Seeder.Timeline;

/// <summary>One group of seed files (e.g., the people) and the events that it runs.</summary>
public interface ISeedPart
{
    /// <summary>Reads the files of the part and lists its events in file order. It writes nothing.</summary>
    /// <param name="folder">The seed folder.</param>
    /// <returns>The events of the part.</returns>
    IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder);

    /// <summary>Runs one event of the part.</summary>
    /// <param name="seedEvent">An event that <see cref="ReadEvents"/> listed.</param>
    /// <param name="context">The keys and the actors of the job.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the event is stored.</returns>
    Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken);
}
