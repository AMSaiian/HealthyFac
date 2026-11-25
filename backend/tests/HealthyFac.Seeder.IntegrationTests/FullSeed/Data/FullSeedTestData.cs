namespace HealthyFac.Seeder.IntegrationTests.FullSeed.Data;

public static class FullSeedTestData
{
    public const int LaterAnchorDays = 7;
    public const int EmployeeCount = 7;
    public const int OpenInviteCount = 2;
    public const int TemplateCount = 7;
    public const int OverrideCount = 2;
    public const int MachineCount = 3;
    public const int MachineDraftCount = 6;
    public const int AppliedMachineDraftCount = 3;
    public const int WorkOrderCount = 13;

    public static TheoryData<string, DateTimeOffset> Anchors => new()
    {
        { "a Saturday at 10:00", SaturdayAnchor },
        { "a Monday at 07:30", MondayAnchor },
        { "a Wednesday at 11:00", WednesdayAnchor },
        { "a Friday at 16:30", FridayAnchor },
    };

    /// <summary>A Monday 07:30 in Kyiv (UTC+3 in May).</summary>
    public static readonly DateTimeOffset MondayAnchor = new(
        year: 2027, month: 5, day: 31, hour: 4, minute: 30, second: 0, TimeSpan.Zero);

    /// <summary>A Wednesday 11:00 in Kyiv (UTC+3 in June).</summary>
    public static readonly DateTimeOffset WednesdayAnchor = new(
        year: 2027, month: 6, day: 2, hour: 8, minute: 0, second: 0, TimeSpan.Zero);

    /// <summary>A Friday 16:30 in Kyiv (UTC+3 in June).</summary>
    public static readonly DateTimeOffset FridayAnchor = new(
        year: 2027, month: 6, day: 4, hour: 13, minute: 30, second: 0, TimeSpan.Zero);

    /// <summary>A Saturday 10:00 in Kyiv (UTC+3 in June).</summary>
    public static readonly DateTimeOffset SaturdayAnchor = new(
        year: 2027, month: 6, day: 5, hour: 7, minute: 0, second: 0, TimeSpan.Zero);
}
