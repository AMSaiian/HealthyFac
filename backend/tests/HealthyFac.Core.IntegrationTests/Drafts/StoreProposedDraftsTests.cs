using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class StoreProposedDraftsTests(CoreDatabaseFixture fixture)
{
    private const string SerialNumber = "SN-100";
    private const string SerialNumberInOtherCase = "sn-100";

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task StoreProposed_ForMachineFileWithoutMachineFields_StoresOnlyChildrenOfTheMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: true, ProposalBuilder.Machine(machineId));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        machineDrafts.Should().BeEmpty();
        metrics.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        statuses.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
    }

    [Fact]
    public async Task StoreProposed_ForMachineFileWithName_StoresAnUpdateDraftAndChildren()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: true, ProposalBuilder.Machine(machineId, name: "New name"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();

        machineDrafts.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        machineDrafts[index: 0].Name.Should().Be(expected: "New name");
        machineDrafts[index: 0].QueuedAt.Should().Be(DraftTestData.QueuedAt);
        metrics.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
    }

    [Fact]
    public async Task StoreProposed_ForCompanyFileWithMatchingSerial_StoresUpdatePathAndOneMatchMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(tenant.CompanyId, DraftTestData.MachineName, SerialNumber);
        var message = ProposalBuilder.Message(
            tenant.CompanyId,
            isMachineFile: false,
            ProposalBuilder.Machine(name: "Renamed", serialNumber: SerialNumberInOtherCase));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        machineDrafts[index: 0].SerialNumber.Should().BeNull();
        metrics.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        matches.Should().ContainSingle();
        matches[index: 0].MachineId.Should().Be(machineId);
        matches[index: 0].FileId.Should().Be(DraftTestData.FileId);
        matches[index: 0].QueuedAt.Should().Be(DraftTestData.QueuedAt);
    }

    [Fact]
    public async Task StoreProposed_ForCompanyFileWithNewSerial_StoresCreateDraftWithChildrenOnTheDraft()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(tenant.CompanyId, DraftTestData.MachineName, SerialNumber);
        var message = ProposalBuilder.Message(
            tenant.CompanyId,
            isMachineFile: false,
            ProposalBuilder.Machine(name: "Lathe", serialNumber: "SN-200"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle().Which.IsCreate.Should().BeTrue();
        metrics.Should().ContainSingle().Which.TargetMachineDraftId.Should().Be(machineDrafts[index: 0].Id);
        metrics[index: 0].TargetMachineId.Should().BeNull();
        statuses.Should().ContainSingle().Which.TargetMachineDraftId.Should().Be(machineDrafts[index: 0].Id);
        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_ForCompanyFileWithTwoEntries_StoresTwoDraftsAndNoMatchMessage()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(tenant.CompanyId, DraftTestData.MachineName, SerialNumber);
        var message = ProposalBuilder.Message(
            tenant.CompanyId,
            isMachineFile: false,
            ProposalBuilder.Machine(name: "One", serialNumber: SerialNumber),
            ProposalBuilder.Machine(name: "Two", serialNumber: "SN-300"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().HaveCount(expected: 2);
        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_ForEmptyMachineOnTheCreatePath_StoresCreateDraftWithoutValues()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var message = ProposalBuilder.Message(tenant.CompanyId, isMachineFile: false, ProposalBuilder.Machine());

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        machineDrafts.Should().ContainSingle();
        machineDrafts[index: 0].Name.Should().BeNull();
        machineDrafts[index: 0].SerialNumber.Should().BeNull();
        machineDrafts[index: 0].IsCreate.Should().BeTrue();
    }

    [Fact]
    public async Task StoreProposed_WithRepeatDelivery_StoresNothingNew()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(tenant.CompanyId, DraftTestData.MachineName, SerialNumber);
        var message = ProposalBuilder.Message(
            tenant.CompanyId,
            isMachineFile: false,
            ProposalBuilder.Machine(name: "Renamed", serialNumber: SerialNumber));
        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle();
        metrics.Should().ContainSingle();
        statuses.Should().ContainSingle();
        matches.Should().ContainSingle();
    }

    [Fact]
    public async Task StoreProposed_WithMessageOfAnotherCompany_ThrowsForbiddenAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var message = ProposalBuilder.Message(otherTenant.CompanyId, isMachineFile: false, ProposalBuilder.Machine());

        var act = () => _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var storedDrafts = await _fixture.LoadMachineDraftsAsync();

        storedDrafts.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_WithMachineOfAnotherCompany_ThrowsNotFoundAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var foreignMachineId = await _fixture.ArrangeMachineAsync(otherTenant.CompanyId);
        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: true, ProposalBuilder.Machine(foreignMachineId));

        var act = () => _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await act.Should().ThrowAsync<NotFoundException>();
        var storedDrafts = await _fixture.LoadMetricDraftsAsync();

        storedDrafts.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_WithDeletedMachine_ThrowsNotFoundAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);
        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: true, ProposalBuilder.Machine(machineId));

        var act = () => _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await act.Should().ThrowAsync<NotFoundException>();
        var storedDrafts = await _fixture.LoadMetricDraftsAsync();

        storedDrafts.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_Always_KeepsTheAttemptAndTheFileOnEveryDraft()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: false, ProposalBuilder.Machine(name: "Pump"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        machineDrafts.Should()
            .OnlyContain(draft => draft.AttemptId == DraftTestData.AttemptId
                && draft.FileId == DraftTestData.FileId
                && draft.State == DraftState.Pending);
        metrics.Should()
            .OnlyContain(draft => draft.AttemptId == DraftTestData.AttemptId
                && draft.FileId == DraftTestData.FileId);
        statuses.Should()
            .OnlyContain(draft => draft.AttemptId == DraftTestData.AttemptId
                && draft.FileId == DraftTestData.FileId);
    }
}
