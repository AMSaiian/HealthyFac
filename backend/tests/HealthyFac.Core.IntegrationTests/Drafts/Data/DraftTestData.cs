namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

public static class DraftTestData
{
    public const string RequestedByName = "Ann Owner";

    public const string MachineName = "AX-200 Rotary Pump";

    public static readonly Guid AttemptId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");
    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a02");
    public static readonly Guid ChatId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a03");
    public static readonly Guid ChatMessageId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a04");

    public static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);
}
