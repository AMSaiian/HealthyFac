using HealthyFac.Seeder.Timeline;
using HealthyFac.Seeder.UnitTests.Timeline.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.UnitTests.Timeline;

[Feature(TestFeatures.Seeding)]
public sealed class TimelineRunnerTests
{
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(id: "Europe/Kyiv");

    [Fact(Explicit = true)]
    public async Task RunAsync_WithEventsOfTwoParts_InitializesFirstThenRunsThemInMomentOrderAndRelaysAfterEach()
    {
        var log = new List<string>();
        var clock = new SeedClock();

        var first = new RecordingSeedPart(
            clock,
            log,
            [
                TimelineTestData.Event(file: "a.json", key: "late", at: "d-1 11:00"),
                TimelineTestData.Event(file: "a.json", key: "early", at: "d-3 09:00"),
            ]);

        var second = new RecordingSeedPart(
            clock, log, [TimelineTestData.Event(file: "b.json", key: "middle", at: "d-2 08:30")]);

        var runner = CreateRunner(clock, log);

        await runner.RunAsync(Plan(first, second), CreateContext(), CancellationToken.None);

        log.Where(entry => !entry.StartsWith(value: "log ", StringComparison.Ordinal))
            .Should()
            .Equal(
                "initialize at 06:59",
                "relay initialization",
                "run a.json#early at 07:00",
                "relay a.json#early",
                "run b.json#middle at 06:30",
                "relay b.json#middle",
                "run a.json#late at 09:00",
                "relay a.json#late");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAFailingPart_StopsAndNamesTheItem()
    {
        var log = new List<string>();
        var clock = new SeedClock();

        var part = new RecordingSeedPart(
            clock,
            log,
            [
                TimelineTestData.Event(file: "a.json", key: "first", at: "d-3 09:00"),
                TimelineTestData.Event(file: "a.json", key: "second", at: "d-2 09:00"),
            ])
        {
            FailingItem = "a.json#first",
        };

        var runner = CreateRunner(clock, log);

        var act = () => runner.RunAsync(Plan(part), CreateContext(), CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                $"*a.json#first*{TimelineTestData.PartError}*");

        log.Where(entry => entry.StartsWith(value: "run ", StringComparison.Ordinal))
            .Should()
            .Equal("run a.json#first at 07:00");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithEventsOfTheSameMoment_RunsThemInFileOrderWithGrowingReads()
    {
        var log = new List<string>();
        var clock = new SeedClock();

        var part = new RecordingSeedPart(
            clock,
            log,
            [
                TimelineTestData.Event(file: "a.json", key: "one", at: "d-1 09:00"),
                TimelineTestData.Event(file: "a.json", key: "two", at: "d-1 09:00"),
                TimelineTestData.Event(file: "a.json", key: "three", at: "d-1 09:00"),
            ]);

        var runner = CreateRunner(clock, log);

        await runner.RunAsync(Plan(part), CreateContext(), CancellationToken.None);

        part.Reads.Select(read => read.Item).Should().Equal("a.json#one", "a.json#two", "a.json#three");
        part.Reads[1].First.Should().BeAfter(part.Reads[0].Last);
        part.Reads[2].First.Should().BeAfter(part.Reads[1].Last);
    }

    private static TimelineRunner CreateRunner(SeedClock clock, List<string> log) =>
        new(
            clock,
            TimelineTestData.Anchor,
            Kyiv,
            _ =>
            {
                log.Add($"initialize at {clock.GetUtcNow():HH:mm}");

                return Task.CompletedTask;
            },
            (item, _) =>
            {
                log.Add($"relay {item}");

                return Task.CompletedTask;
            },
            line => log.Add($"log {line}"));

    private static SeedContext CreateContext() => new() { Anchor = TimelineTestData.Anchor, Zone = Kyiv };

    private static List<PlannedEvent> Plan(params RecordingSeedPart[] parts) =>
    [
        .. parts.SelectMany(part => part.ReadEvents(null!)
            .Select(seedEvent => new PlannedEvent
            {
                Part = part,
                Event = seedEvent,
            })),
    ];
}
