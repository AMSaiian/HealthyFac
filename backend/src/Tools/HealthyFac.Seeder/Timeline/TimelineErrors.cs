using System.Globalization;

namespace HealthyFac.Seeder.Timeline;

/// <summary>
/// Defines the error messages of the seed timeline.
/// </summary>
public static class TimelineErrors
{
    public static string ClockWouldGoBack(string item, DateTimeOffset moment, DateTimeOffset lastRead) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"The event {item} lies at {moment:O}, which is earlier than the clock position {lastRead:O}.");

    public const string ClockNotEntered = "The seed clock was read before the first event was entered.";

    public static string EventFailed(string item, string error) => $"The event {item} failed: {error}";

    public static string KeyUnknown(string key) => $"No earlier event created the key {key}.";

    public static string ActorUnknown(string person) => $"The person {person} has no actor.";
}
