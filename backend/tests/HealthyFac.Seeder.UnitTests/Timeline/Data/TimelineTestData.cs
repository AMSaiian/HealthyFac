using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder.UnitTests.Timeline.Data;

public static class TimelineTestData
{
    public const string PartError = "The part could not store the item.";

    public static readonly DateTimeOffset Anchor = new(
        year: 2027, month: 3, day: 24, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public static SeedEvent Event(string file, string key, string at) =>
        new() { At = RelativeMoment.Parse(at), Item = SeedItem.Of(file, key) };
}
