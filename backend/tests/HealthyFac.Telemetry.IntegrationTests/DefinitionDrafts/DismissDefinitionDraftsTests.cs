using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class DismissDefinitionDraftsTests(TelemetryDatabaseFixture fixture)
{
    private const int OpenDraftsOfAMachine = 3;

    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DismissMachineDrafts_WithOpenDrafts_DismissesThemAndKeepsTheOthers()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var otherMachineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await ArrangeOpenAndAppliedDraftsAsync(companyId, machineId);
        await ArrangeOpenAndAppliedDraftsAsync(companyId, otherMachineId);

        var response = await _fixture.DismissDefinitionDraftsAsync(companyId, machineId, attemptId: null);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        response.DismissedCount.Should().Be(OpenDraftsOfAMachine);

        metrics.Where(draft => draft.TargetMachineId == machineId)
            .Select(draft => draft.State)
            .Should()
            .BeEquivalentTo([DefinitionDraftState.Dismissed, DefinitionDraftState.Applied]);

        statuses.Where(draft => draft.TargetMachineId == machineId)
            .Select(draft => draft.State)
            .Should()
            .OnlyContain(state => state == DefinitionDraftState.Dismissed);

        metrics.Where(draft => draft.TargetMachineId == otherMachineId)
            .Select(draft => draft.State)
            .Should()
            .BeEquivalentTo([DefinitionDraftState.Pending, DefinitionDraftState.Applied]);
    }

    [Fact]
    public async Task DismissAttemptDrafts_LeavesTheChatDraftsOfTheSameMachine()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId, ProposalBuilder.ChatMessage(companyId, machineId));

        var response = await _fixture.DismissDefinitionDraftsAsync(
            companyId, machineId: null, DefinitionDraftTestData.AttemptId);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var chatMetric = metrics.Single(draft => draft.Proposal.Source == DraftSource.Chat);

        response.DismissedCount.Should().Be(expected: 1);
        chatMetric.State.Should().Be(DefinitionDraftState.Pending);
    }

    [Fact]
    public async Task DismissAttemptDrafts_DismissesTheOpenDraftsOfEveryMachineOfTheAttempt()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        var otherMachineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await ArrangeOpenAndAppliedDraftsAsync(companyId, machineId);
        await ArrangeOpenAndAppliedDraftsAsync(companyId, otherMachineId);

        var response = await _fixture.DismissDefinitionDraftsAsync(
            companyId, machineId: null, DefinitionDraftTestData.AttemptId);

        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        response.DismissedCount.Should().Be(OpenDraftsOfAMachine * 2);

        metrics.Concat<IDefinitionDraft>(statuses)
            .Should()
            .NotContain(draft => draft.State == DefinitionDraftState.Pending
                || draft.State == DefinitionDraftState.AwaitingTarget);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAMachineOfAnotherCompany_ThrowsNotFound()
    {
        var foreignMachineId = await _fixture.RegisterTenantMachineAsync(Guid.CreateVersion7());

        var act = () => _fixture.DismissDefinitionDraftsAsync(
            DefinitionDraftTestData.CompanyId, foreignMachineId, attemptId: null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DismissAttemptDrafts_InParallelWithItself_DismissesOnceAndTheOtherCallConflicts()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await ArrangeOpenAndAppliedDraftsAsync(companyId, machineId);
        var responses = new List<DismissDefinitionDraftsResponse>();
        using var race = _fixture.SaveGate.StartRace(participantCount: 2);

        var failures = await Task.WhenAll(
            race.RunAsync(async () => responses.Add(
                await _fixture.DismissDefinitionDraftsAsync(
                    companyId, machineId: null, DefinitionDraftTestData.AttemptId))),
            race.RunAsync(async () => responses.Add(
                await _fixture.DismissDefinitionDraftsAsync(
                    companyId, machineId: null, DefinitionDraftTestData.AttemptId))));

        race.SaveCounts.Should().Equal(1, 1);
        responses.Should().ContainSingle().Which.DismissedCount.Should().Be(OpenDraftsOfAMachine);

        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DefinitionDraftErrors.DraftChanged);
    }

    private async Task ArrangeOpenAndAppliedDraftsAsync(Guid companyId, Guid machineId)
    {
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: true);
        await _fixture.StorePendingStatusDraftOnMachineAsync(companyId, machineId);
        await _fixture.StoreAwaitingStatusDraftOnMachineAsync(companyId, machineId);
    }
}
