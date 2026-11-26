using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

public static class MarkDraftOnApplyTestData
{
    public const string StatusCode = "E-300";

    /// <summary>
    /// The case name and one write that names a draft. The arguments are the fixture, the company, the machine, the
    /// draft, the index of the caller (0 or 1), and the IDs of 2 stored statuses, one for each caller.
    /// </summary>
    public static TheoryData<
        string,
        Func<TelemetryDatabaseFixture, Guid, Guid, Guid, int, IReadOnlyList<Guid>, Task>> ParallelWrites => new()
    {
        {
            "create",
            (fixture, companyId, machineId, draftId, index, _) => fixture.CreateStatusDefinitionAsync(
                companyId, machineId, $"R-{index}", draftId: draftId)
        },
        {
            "update",
            (fixture, companyId, machineId, draftId, index, statusIds) => fixture.UpdateStatusDefinitionAsync(
                companyId, machineId, statusIds[index], $"R-{index}", draftId: draftId)
        },
        {
            "batch",
            (fixture, companyId, machineId, draftId, index, _) => fixture.ApplyStatusChangesAsync(
                companyId,
                machineId,
                [new StatusDefinitionChange { Code = $"R-{index}", DraftId = draftId }])
        },
    };

    /// <summary>
    /// The case name and a call that applies a definition with a draft that is not pending: the arrange, then the
    /// write. The arguments are the fixture, the company, and the machine.
    /// </summary>
    public static TheoryData<string, Func<TelemetryDatabaseFixture, Guid, Guid, Task>> NotPendingDrafts => new()
    {
        {
            "awaitingTarget",
            async (fixture, companyId, machineId) =>
            {
                var draftId = await fixture.StoreAwaitingStatusDraftOnMachineAsync(companyId, machineId);

                await fixture.ApplyStatusChangesAsync(
                    companyId,
                    machineId,
                    [new StatusDefinitionChange { Code = StatusCode, DraftId = draftId }]);
            }
        },
        {
            "applied",
            async (fixture, companyId, machineId) =>
            {
                var draftId = await fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: true);

                await fixture.ApplyMetricChangesAsync(
                    new MachineTenant(companyId),
                    machineId,
                    expectedRevision: 0,
                    [MetricDefinitionTestData.NewNumeric() with { DraftId = draftId }]);
            }
        },
        {
            "dismissed",
            async (fixture, companyId, machineId) =>
            {
                var draftId = await fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
                await fixture.DismissMetricDraftAsync(draftId);

                await fixture.ApplyMetricChangesAsync(
                    new MachineTenant(companyId),
                    machineId,
                    expectedRevision: 0,
                    [MetricDefinitionTestData.NewNumeric() with { DraftId = draftId }]);
            }
        },
    };
}
