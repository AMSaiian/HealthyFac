using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.Ingest)]
public sealed class StoreProposedDraftsTests(CoreDatabaseFixture fixture)
{
    private const string SerialNumber = "SN-100";
    private const string SerialNumberInOtherCase = "sn-100";

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task StoreProposed_ForMachineFileWithOnlyDefinitions_StoresNoMachineDraftAndSendsNoResolution()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);

        var message = ProposalBuilder.Message(tenant.CompanyId, isMachineFile: true) with
        {
            Metrics = [ProposalBuilder.Metric(machineId)],
        };

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();

        machineDrafts.Should().BeEmpty();
        resolutions.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_ForMachineFileWithName_StoresAnUpdateDraft()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);

        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: true, ProposalBuilder.Machine(machineId, name: "New name"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        machineDrafts.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        machineDrafts[0].Name.Should().Be(expected: "New name");
        machineDrafts[0].Proposal.QueuedAt.Should().Be(DraftTestData.QueuedAt);
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
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        machineDrafts[0].SerialNumber.Should().BeNull();
        matches.Should().ContainSingle();
        matches[0].MachineId.Should().Be(machineId);
        matches[0].FileId.Should().Be(DraftTestData.FileId);
        matches[0].QueuedAt.Should().Be(DraftTestData.QueuedAt);
    }

    [Fact]
    public async Task StoreProposed_ForCompanyFileWithNewSerial_StoresACreateDraft()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(tenant.CompanyId, DraftTestData.MachineName, SerialNumber);

        var message = ProposalBuilder.Message(
            tenant.CompanyId,
            isMachineFile: false,
            ProposalBuilder.Machine(name: "Lathe", serialNumber: "SN-200"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle().Which.IsCreate.Should().BeTrue();
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
        var entry = ProposalBuilder.Machine();

        var message = ProposalBuilder.Message(tenant.CompanyId, isMachineFile: false, entry) with
        {
            Metrics = [ProposalBuilder.Metric(machineDraftId: entry.DraftId)],
        };

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        machineDrafts.Should().ContainSingle();
        machineDrafts[0].Name.Should().BeNull();
        machineDrafts[0].SerialNumber.Should().BeNull();
        machineDrafts[0].IsCreate.Should().BeTrue();
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
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle();
        matches.Should().ContainSingle();
    }

    [Fact]
    public async Task StoreProposed_WithRepeatDelivery_StoresOneProposalOfTheAttempt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: false, ProposalBuilder.Machine(name: "Pump"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var proposals = await _fixture.LoadDraftProposalsAsync();

        var proposal = proposals.Should().ContainSingle().Subject;
        proposal.Id.Should().Be(DraftTestData.AttemptId);
        proposal.Source.Should().Be(DraftSource.Ingest);
        proposal.FileId.Should().Be(DraftTestData.FileId);
    }

    [Fact]
    public async Task StoreProposed_WithACreateEntryAndATargetEntry_SendsAnAcceptedResolutionWithBothTargets()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var withTarget = ProposalBuilder.Machine(machineId, name: "Pump");
        var withoutTarget = ProposalBuilder.Machine(name: "Lathe", serialNumber: "SN-NEW");
        var message = ProposalBuilder.Message(tenant.CompanyId, isMachineFile: false, withTarget, withoutTarget);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();
        var resolution = resolutions.Should().ContainSingle().Subject;

        resolution.Resolution.Should().Be(DraftResolution.Accepted);
        resolution.AttemptId.Should().Be(DraftTestData.AttemptId);

        resolution.Entries.Should()
            .BeEquivalentTo(
            [
                new ResolvedEntry { EntryId = withTarget.DraftId, MachineId = machineId },
                new ResolvedEntry { EntryId = withoutTarget.DraftId, IsOnMachineDraft = true },
            ]);
    }

    [Fact]
    public async Task StoreProposed_WithoutMetricsAndStatuses_StillSendsTheResolution()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var message = ProposalBuilder.Message(
            tenant.CompanyId,
            isMachineFile: false,
            ProposalBuilder.Machine(name: "Pump"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();

        resolutions.Should().ContainSingle().Which.Resolution.Should().Be(DraftResolution.Accepted);
    }

    [Fact]
    public async Task StoreProposed_WithRepeatDeliveryOfAStoredProposal_SendsNoSecondResolution()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: false, ProposalBuilder.Machine(name: "Pump"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();

        resolutions.Should().ContainSingle();
    }

    [Fact]
    public async Task StoreProposed_WithMessageOfAnotherCompany_CommitsARejectedResolutionAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var entry = ProposalBuilder.Machine(name: "Pump");
        var message = ProposalBuilder.Message(otherTenant.CompanyId, isMachineFile: false, entry);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await AssertRejectedAsync(otherTenant.CompanyId, entry.DraftId);
    }

    [Fact]
    public async Task StoreProposed_WithMachineOfAnotherCompany_CommitsARejectedResolutionAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync();
        var foreignMachineId = await _fixture.ArrangeMachineAsync(otherTenant.CompanyId);
        var entry = ProposalBuilder.Machine(foreignMachineId, name: "Pump");
        var message = ProposalBuilder.Message(tenant.CompanyId, isMachineFile: true, entry);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await AssertRejectedAsync(tenant.CompanyId, entry.DraftId);
    }

    [Fact]
    public async Task StoreProposed_WithDeletedMachine_CommitsARejectedResolutionAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);
        var entry = ProposalBuilder.Machine(machineId, name: "Pump");
        var message = ProposalBuilder.Message(tenant.CompanyId, isMachineFile: true, entry);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await AssertRejectedAsync(tenant.CompanyId, entry.DraftId);
    }

    [Fact]
    public async Task StoreProposed_Always_KeepsTheAttemptAndTheFileOnEveryDraft()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var message = ProposalBuilder.Message(
            tenant.CompanyId, isMachineFile: false, ProposalBuilder.Machine(name: "Pump"));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        machineDrafts.Should()
            .OnlyContain(draft => draft.Proposal.AttemptId == DraftTestData.AttemptId
                && draft.Proposal.FileId == DraftTestData.FileId
                && draft.State == DraftState.Pending);
    }

    [Fact]
    public async Task StoreProposed_ForAChatCreateDraft_StoresItWithTheChatProposalAndResolvesIt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var entry = ProposalBuilder.Machine(name: "Lathe");
        var message = ProposalBuilder.ChatMessage(tenant.CompanyId, entry);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var proposals = await _fixture.LoadDraftProposalsAsync();
        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();

        var proposal = proposals.Should().ContainSingle().Subject;
        proposal.Source.Should().Be(DraftSource.Chat);
        proposal.Id.Should().Be(DraftTestData.ChatMessageId);
        proposal.ChatId.Should().Be(DraftTestData.ChatId);
        machineDrafts.Should().ContainSingle().Which.ProposalId.Should().Be(proposal.Id);
        var resolution = resolutions.Should().ContainSingle().Subject;
        resolution.Source.Should().Be(ResolutionSource.Chat);
        resolution.AttemptId.Should().BeNull();
        resolution.Resolution.Should().Be(DraftResolution.Accepted);

        resolution.Entries.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new ResolvedEntry { EntryId = entry.DraftId, IsOnMachineDraft = true });
    }

    [Fact]
    public async Task StoreProposed_ForAChatUpdateDraftOfAnActiveMachine_AcceptsItOnThatMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var entry = ProposalBuilder.Machine(machineId, name: "New name");
        var message = ProposalBuilder.ChatMessage(tenant.CompanyId, entry);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();

        machineDrafts.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        var resolution = resolutions.Should().ContainSingle().Subject;
        resolution.Resolution.Should().Be(DraftResolution.Accepted);

        resolution.Entries.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new ResolvedEntry { EntryId = entry.DraftId, MachineId = machineId });
    }

    [Fact]
    public async Task StoreProposed_ForAChatUpdateDraftOfADeletedMachine_RejectsTheProposalAndStoresNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);
        var entry = ProposalBuilder.Machine(machineId, name: "New name");
        var message = ProposalBuilder.ChatMessage(tenant.CompanyId, entry);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var proposals = await _fixture.LoadDraftProposalsAsync();
        await AssertRejectedAsync(tenant.CompanyId, entry.DraftId);
        proposals.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_ForAChatDraftWithTheSerialNumberOfAMachine_StaysACreateDraftWithoutAMatch()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(tenant.CompanyId, DraftTestData.MachineName, SerialNumber);

        var message = ProposalBuilder.ChatMessage(
            tenant.CompanyId, ProposalBuilder.Machine(name: "Renamed", serialNumber: SerialNumber));

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();

        machineDrafts.Should().ContainSingle().Which.IsCreate.Should().BeTrue();
        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_ForAChatProposalWithoutMachines_StoresTheProposalAndSendsNoResolution()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var message = ProposalBuilder.ChatMessage(tenant.CompanyId) with
        {
            Metrics = [ProposalBuilder.Metric(Guid.CreateVersion7())],
        };

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var proposals = await _fixture.LoadDraftProposalsAsync();
        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();

        proposals.Should().ContainSingle().Which.Source.Should().Be(DraftSource.Chat);
        resolutions.Should().BeEmpty();
    }

    [Fact]
    public async Task StoreProposed_ForARepeatOfAChatProposal_SendsNoSecondResolution()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var message = ProposalBuilder.ChatMessage(tenant.CompanyId, ProposalBuilder.Machine(name: "Lathe"));
        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        await _fixture.StoreProposedDraftsAsync(tenant.CompanyId, message);

        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        resolutions.Should().ContainSingle();
        machineDrafts.Should().ContainSingle();
    }

    private async Task AssertRejectedAsync(Guid companyId, Guid entryId)
    {
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var matches = await _fixture.ReadOutboxMessagesAsync<FileMachineMatched>();
        var resolutions = await _fixture.ReadOutboxMessagesAsync<DraftsResolved>();
        var resolution = resolutions.Should().ContainSingle().Subject;

        machineDrafts.Should().BeEmpty();
        matches.Should().BeEmpty();
        resolution.Resolution.Should().Be(DraftResolution.Rejected);
        resolution.CompanyId.Should().Be(companyId);

        resolution.Entries.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new ResolvedEntry { EntryId = entryId });
    }
}
