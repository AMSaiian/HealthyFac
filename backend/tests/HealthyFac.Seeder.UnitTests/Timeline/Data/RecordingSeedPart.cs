using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder.UnitTests.Timeline.Data;

/// <summary>A part that records the clock read at the start of each event into a shared log.</summary>
public sealed class RecordingSeedPart : ISeedPart
{
    private readonly SeedClock _clock;
    private readonly List<string> _log;
    private readonly IReadOnlyList<SeedEvent> _events;

    public RecordingSeedPart(SeedClock clock, List<string> log, IReadOnlyList<SeedEvent> events)
    {
        _clock = clock;
        _log = log;
        _events = events;
    }

    public string? FailingItem { get; init; }

    public List<(string Item, DateTimeOffset First, DateTimeOffset Last)> Reads { get; } = [];

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder) => _events;

    public Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken)
    {
        var first = _clock.GetUtcNow();
        _log.Add($"run {seedEvent.Item} at {first:HH:mm}");
        Reads.Add((seedEvent.Item, first, _clock.GetUtcNow()));
        if (seedEvent.Item == FailingItem)
        {
            throw new InvalidOperationException(TimelineTestData.PartError);
        }

        return Task.CompletedTask;
    }
}
