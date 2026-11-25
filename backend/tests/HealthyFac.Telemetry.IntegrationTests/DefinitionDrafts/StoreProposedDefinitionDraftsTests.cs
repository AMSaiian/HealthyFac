using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.Ingest)]
[Feature(TestFeatures.SignalModel)]
public sealed class StoreProposedDefinitionDraftsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Store_WithTwoCreateDrafts_StoresEveryChildAwaitingItsEntry()
    {
        var first = ProposalBuilder.Machine();
        var second = ProposalBuilder.Machine();
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, first, second);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Should().HaveCount(expected: 2);
        statuses.Should().HaveCount(expected: 2);

        metrics.Concat<IDefinitionDraft>(statuses)
            .Should()
            .OnlyContain(draft => draft.State == DefinitionDraftState.AwaitingTarget);

        metrics.Select(draft => draft.ProposalEntryId).Should().BeEquivalentTo([first.DraftId, second.DraftId]);
        entries.Select(entry => entry.Id).Should().BeEquivalentTo([first.DraftId, second.DraftId]);
        entries.Should().OnlyContain(entry => entry.State == ProposalEntryState.Awaiting);
    }

    [Fact]
    public async Task Store_ForAMachineOfTheCompany_StoresTheChildrenPendingOnItWithoutAnEntry()
    {
        var machineId = await _fixture.RegisterTenantMachineAsync(DefinitionDraftTestData.CompanyId);
        var message = ProposalBuilder.OnMachineMessage(DefinitionDraftTestData.CompanyId, machineId);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Concat<IDefinitionDraft>(statuses)
            .Should()
            .OnlyContain(draft => draft.State == DefinitionDraftState.Pending
                && draft.TargetMachineId == machineId
                && draft.ProposalEntryId == null);

        metrics.Should().ContainSingle();
        statuses.Should().ContainSingle();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Store_ForAMachineWithADeletedRegistration_StoresNothingForIt()
    {
        var machineId = await _fixture.RegisterTenantMachineAsync(DefinitionDraftTestData.CompanyId);
        await _fixture.DeleteMachineAsync(DefinitionDraftTestData.CompanyId, machineId);
        _fixture.ClearLogs();
        var message = ProposalBuilder.OnMachineMessage(DefinitionDraftTestData.CompanyId, machineId);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var warnings = _fixture.ReadWarnings<StoreProposedDefinitionDraftsRequestHandler>();

        metrics.Should().BeEmpty();
        statuses.Should().BeEmpty();
        warnings.Should().ContainSingle();
    }

    [Fact]
    public async Task Store_ForAnUnknownMachine_ThrowsNotFoundAndStoresNothing()
    {
        var message = ProposalBuilder.OnMachineMessage(DefinitionDraftTestData.CompanyId, Guid.CreateVersion7());

        var act = () => _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        await act.Should().ThrowAsync<NotFoundException>();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task Store_ForAChatProposal_StoresTheChatProposalAndTheChildrenOnTheMachine()
    {
        var machineId = await _fixture.RegisterTenantMachineAsync(DefinitionDraftTestData.CompanyId);
        var message = ProposalBuilder.ChatMessage(DefinitionDraftTestData.CompanyId, machineId);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var proposals = await _fixture.LoadDraftProposalsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();

        var proposal = proposals.Should().ContainSingle().Subject;
        proposal.Source.Should().Be(DraftSource.Chat);
        proposal.Id.Should().Be(DefinitionDraftTestData.ChatMessageId);
        proposal.ChatId.Should().Be(DefinitionDraftTestData.ChatId);
        metrics.Should().ContainSingle().Which.ProposalId.Should().Be(proposal.Id);
    }

    [Fact]
    public async Task Store_Twice_StoresNothingNew()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());
        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Should().HaveCount(expected: 1);
        statuses.Should().HaveCount(expected: 1);
        entries.Should().HaveCount(expected: 1);
    }

    [Fact]
    public async Task Store_Twice_StoresOneProposalOfTheAttempt()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());
        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var proposals = await _fixture.LoadDraftProposalsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();

        var proposal = proposals.Should().ContainSingle().Subject;
        proposal.Id.Should().Be(DefinitionDraftTestData.AttemptId);
        proposal.Source.Should().Be(DraftSource.Ingest);
        proposal.FileId.Should().Be(DefinitionDraftTestData.FileId);
        metrics.Should().ContainSingle().Which.ProposalId.Should().Be(proposal.Id);
    }

    [Fact]
    public async Task Store_AfterASoftDelete_DoesNotStoreTheDraftAgain()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());
        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);
        var stored = await _fixture.LoadMetricDraftsAsync();
        await _fixture.SoftDeleteMetricDraftAsync(stored.Single().Id);

        await _fixture.StoreProposedDefinitionDraftsAsync(DefinitionDraftTestData.CompanyId, message);

        var metrics = await _fixture.LoadMetricDraftsAsync();

        metrics.Should().ContainSingle().Which.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Store_ForAnotherCompany_ThrowsAndStoresNothing()
    {
        var message = ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposalBuilder.Machine());

        var act = () => _fixture.StoreProposedDefinitionDraftsAsync(Guid.CreateVersion7(), message);

        await act.Should().ThrowAsync<ForbiddenAccessException>();

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var entries = await _fixture.LoadProposalEntriesAsync();

        metrics.Should().BeEmpty();
        entries.Should().BeEmpty();
    }
}
