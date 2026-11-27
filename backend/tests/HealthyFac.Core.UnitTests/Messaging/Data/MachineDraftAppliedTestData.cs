namespace HealthyFac.Core.UnitTests.Messaging.Data;

public static class MachineDraftAppliedTestData
{
    public static readonly Guid Id = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e01");

    private static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    /// <summary>The case name, the file, the queue time, and whether the message is valid.</summary>
    public static TheoryData<string, Guid?, DateTime?, bool> FileAndTime => new()
    {
        { "BothSet", Id, QueuedAt, true },
        { "NeitherSet", null, null, true },
        { "FileWithoutTime", Id, null, false },
        { "TimeWithoutFile", null, QueuedAt, false },
        { "EmptyFile", Guid.Empty, QueuedAt, false },
    };
}
