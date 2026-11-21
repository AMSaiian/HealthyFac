namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class DemoDraftSeedTestData
{
    public const int MachineDraftCount = 3;
    public const string WorkOrderDraftTitle = "Inspect the spindle bearing";
    public const int WorkOrderDraftEstimateMinutes = 60;
    public static readonly Guid WorkOrderDraftId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c21");
    public static readonly Guid CaseId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c41");
    public const string CncMachineName = "MTC-420";
    public const string CncSerialNumber = "MTC420-24-0187";
}
