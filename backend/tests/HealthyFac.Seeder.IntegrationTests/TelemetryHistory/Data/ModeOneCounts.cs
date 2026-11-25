using HealthyFac.Seeder.IntegrationTests.Cases.Data;
using HealthyFac.Seeder.IntegrationTests.FullSeed.Data;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

/// <summary>The counts of the mode-1 seed that a mode-2 seed keeps.</summary>
public sealed class ModeOneCounts
{
    public static readonly ModeOneCounts Expected = new()
    {
        Employees = FullSeedTestData.EmployeeCount,
        AcceptedInvites = FullSeedTestData.EmployeeCount,
        OpenInvites = FullSeedTestData.OpenInviteCount,
        Templates = FullSeedTestData.TemplateCount,
        Overrides = FullSeedTestData.OverrideCount,
        Machines = FullSeedTestData.MachineCount,
        MachineDrafts = FullSeedTestData.MachineDraftCount,
        WorkOrders = FullSeedTestData.WorkOrderCount,
        WorkOrderDraftId = CasesTestData.DraftId,
    };

    public required int Employees { get; init; }

    public required int AcceptedInvites { get; init; }

    public required int OpenInvites { get; init; }

    public required int Templates { get; init; }

    public required int Overrides { get; init; }

    public required int Machines { get; init; }

    public required int MachineDrafts { get; init; }

    public required int WorkOrders { get; init; }

    public required Guid WorkOrderDraftId { get; init; }
}
