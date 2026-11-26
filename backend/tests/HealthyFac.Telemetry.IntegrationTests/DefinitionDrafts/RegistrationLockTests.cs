using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.Machines)]
public sealed class RegistrationLockTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Follow_OfAppliedMachineDraftWhileMachineDeletedHoldsTheLock_DismissesTheChildren()
    {
        var ids = await ArrangeAwaitingChildrenOnRegisteredMachineAsync();

        await _fixture.RunWithRegistrationLockHeldAsync(
            services => services.GetRequiredService<IRequestHandler<MachineDeleted>>()
                .HandleAsync(
                    new MachineDeleted { MachineId = ids.MachineId, CompanyId = DefinitionDraftTestData.CompanyId }),
            () => ProposalEntryFlowTestData.Applied(_fixture, ids));

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var status = (await _fixture.LoadStatusDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Dismissed);
        status.State.Should().Be(DefinitionDraftState.Dismissed);
    }

    [Fact]
    public async Task Unregister_WhileTheAppliedMachineDraftHoldsTheLock_DismissesTheChildrenItMovedToPending()
    {
        var ids = await ArrangeAwaitingChildrenOnRegisteredMachineAsync();

        await _fixture.RunWithRegistrationLockHeldAsync(
            services => services.GetRequiredService<IRequestHandler<MachineDraftApplied>>()
                .HandleAsync(
                    new MachineDraftApplied
                    {
                        DraftId = ids.EntryId,
                        FileId = DefinitionDraftTestData.FileId,
                        MachineId = ids.MachineId,
                        CompanyId = DefinitionDraftTestData.CompanyId,
                        QueuedAt = DateTime.UtcNow,
                    }),
            () => _fixture.DeleteMachineAsync(DefinitionDraftTestData.CompanyId, ids.MachineId));

        var metric = (await _fixture.LoadMetricDraftsAsync()).Single();
        var status = (await _fixture.LoadStatusDraftsAsync()).Single();

        metric.State.Should().Be(DefinitionDraftState.Dismissed);
        status.State.Should().Be(DefinitionDraftState.Dismissed);
    }

    private async Task<FlowIds> ArrangeAwaitingChildrenOnRegisteredMachineAsync()
    {
        var ids = FlowIds.Create();

        await _fixture.RegisterMachineAsync(
            MachineRegistrationTestData.CreateMessage() with
            {
                MachineId = ids.MachineId,
                CompanyId = DefinitionDraftTestData.CompanyId,
            });

        await ProposalEntryFlowTestData.Proposed(_fixture, ids);
        await ProposalEntryFlowTestData.AcceptedOnMachineDraft(_fixture, ids);

        return ids;
    }
}
