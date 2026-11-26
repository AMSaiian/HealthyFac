using FluentValidation;
using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.IntegrationTests.WorkOrders.Data;
using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.WorkOrders;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
[Feature(TestFeatures.Drafts)]
public sealed class WorkOrderDraftTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Propose_Twice_StoresOnePendingDraft()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());

        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);

        var drafts = await _fixture.LoadWorkOrderDraftsAsync();
        var draft = drafts.Should().ContainSingle().Subject;
        draft.Id.Should().Be(message.DraftId);
        draft.State.Should().Be(DraftState.Pending);
        draft.MachineId.Should().Be(machineId);
    }

    [Fact]
    public async Task Receive_TheProposalThroughTheInbox_StoresOnePendingDraftAndMarksTheRowProcessed()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());

        var outcome = await _fixture.ReceiveWorkOrderDraftThroughInboxAsync(message);

        var drafts = await _fixture.LoadWorkOrderDraftsAsync();
        outcome.IsProcessed.Should().BeTrue(outcome.Error);
        drafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Pending);
    }

    [Fact]
    public async Task Receive_TheProposalForAMachineOfAnotherCompanyThroughTheInbox_FailsTheRowAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(otherTenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());

        var outcome = await _fixture.ReceiveWorkOrderDraftThroughInboxAsync(message);

        var drafts = await _fixture.LoadWorkOrderDraftsAsync();
        outcome.IsProcessed.Should().BeFalse();
        outcome.AttemptCount.Should().BeGreaterThan(expected: 0);
        drafts.Should().BeEmpty();
    }

    [Fact]
    public async Task Propose_ForAMachineOfAnotherCompany_StoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(otherTenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());

        var act = () => _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);

        await act.Should().ThrowAsync<NotFoundException>();
        var drafts = await _fixture.LoadWorkOrderDraftsAsync();
        drafts.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_WithAPendingDraft_CreatesTheWorkOrderMarksTheDraftAndAddsTheMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);

        var response = await _fixture.ApplyWorkOrderDraftAsync(
            WorkOrderDraftTestData.Apply(tenant.CompanyId, message.DraftId));

        var draft = (await _fixture.LoadWorkOrderDraftsAsync()).Single();
        var messages = await _fixture.ReadDraftOutboxAsync<WorkOrderDraftApplied>();
        var workOrderCount = await _fixture.CountWorkOrdersAsync(response.WorkOrderId);
        draft.State.Should().Be(DraftState.Applied);
        draft.AppliedWorkOrderId.Should().Be(response.WorkOrderId);
        workOrderCount.Should().Be(expected: 1);
        var applied = messages.Should().ContainSingle().Subject;
        applied.DraftId.Should().Be(message.DraftId);
        applied.WorkOrderId.Should().Be(response.WorkOrderId);
        applied.CaseId.Should().Be(message.CaseId);
    }

    [Fact]
    public async Task Apply_WithAnEstimateOffTheGrid_FailsAndChangesNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);

        var act = () => _fixture.ApplyWorkOrderDraftAsync(
            WorkOrderDraftTestData.Apply(
                tenant.CompanyId, message.DraftId, WorkOrderDraftTestData.OffGridEstimate));

        await act.Should().ThrowAsync<ValidationException>();
        var draft = (await _fixture.LoadWorkOrderDraftsAsync()).Single();
        var messages = await _fixture.ReadDraftOutboxAsync<WorkOrderDraftApplied>();
        draft.State.Should().Be(DraftState.Pending);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_Twice_FailsTheSecondTimeWithUnprocessable()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);
        await _fixture.ApplyWorkOrderDraftAsync(WorkOrderDraftTestData.Apply(tenant.CompanyId, message.DraftId));

        var act = () => _fixture.ApplyWorkOrderDraftAsync(
            WorkOrderDraftTestData.Apply(tenant.CompanyId, message.DraftId));

        await act.Should().ThrowAsync<UnprocessableException>();
        var messages = await _fixture.ReadDraftOutboxAsync<WorkOrderDraftApplied>();
        messages.Should().ContainSingle();
    }

    [Fact]
    public async Task Apply_WithAStaleRowVersion_FailsWithConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);

        var act = () => _fixture.ApplyWorkOrderDraftAsync(
            WorkOrderDraftTestData.Apply(
                tenant.CompanyId, message.DraftId, rowVersion: WorkOrderDraftTestData.StaleRowVersion));

        await act.Should().ThrowAsync<ConflictException>();
        var draft = (await _fixture.LoadWorkOrderDraftsAsync()).Single();
        draft.State.Should().Be(DraftState.Pending);
    }

    [Fact]
    public async Task Dismiss_WithAPendingDraft_DismissesItAndAddsTheMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);

        await _fixture.DismissWorkOrderDraftAsync(
            new DismissWorkOrderDraftRequest { CompanyId = tenant.CompanyId, Id = message.DraftId });

        var draft = (await _fixture.LoadWorkOrderDraftsAsync()).Single();
        var messages = await _fixture.ReadDraftOutboxAsync<WorkOrderDraftDismissed>();
        draft.State.Should().Be(DraftState.Dismissed);
        messages.Should().ContainSingle().Which.CaseId.Should().Be(message.CaseId);
    }

    [Fact]
    public async Task Dismiss_Twice_FailsTheSecondTimeWithUnprocessable()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = WorkOrderDraftTestData.Proposal(tenant.CompanyId, machineId, Guid.CreateVersion7());
        await _fixture.ProposeWorkOrderDraftAsync(tenant.CompanyId, message);
        var request = new DismissWorkOrderDraftRequest { CompanyId = tenant.CompanyId, Id = message.DraftId };
        await _fixture.DismissWorkOrderDraftAsync(request);

        var act = () => _fixture.DismissWorkOrderDraftAsync(request);

        await act.Should().ThrowAsync<UnprocessableException>();
        var messages = await _fixture.ReadDraftOutboxAsync<WorkOrderDraftDismissed>();
        messages.Should().ContainSingle();
    }
}
