namespace HealthyFac.Seeder.IntegrationTests.FullSeed.Data;

public static class FullSeedTestData
{
    public const int EmployeeCount = 7;
    public const int OpenInviteCount = 2;
    public const int TemplateCount = 7;
    public const int OverrideCount = 2;
    public const int MachineCount = 3;
    public const int MachineDraftCount = 6;
    public const int AppliedMachineDraftCount = 3;
    public const int WorkOrderCount = 13;

    /// <summary>A Saturday 10:00 in Kyiv (UTC+3 in June).</summary>
    public static readonly DateTimeOffset SaturdayAnchor = new(
        year: 2027, month: 6, day: 5, hour: 7, minute: 0, second: 0, TimeSpan.Zero);
}
