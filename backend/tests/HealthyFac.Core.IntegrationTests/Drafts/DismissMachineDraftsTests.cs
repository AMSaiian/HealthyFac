using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.Machines)]
public sealed class DismissMachineDraftsTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DismissMachineDrafts_WithPendingDrafts_DismissesThemAndKeepsTheOthers()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var otherMachineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, otherMachineId);

        var response = await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        response.DismissedCount.Should().Be(expected: 1);
        machineDrafts.Single(draft => draft.TargetMachineId == machineId).State.Should().Be(DraftState.Dismissed);
        machineDrafts.Single(draft => draft.TargetMachineId == otherMachineId).State.Should().Be(DraftState.Pending);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAnAttempt_DismissesThePendingDraftsOfEveryMachineOfThatAttempt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var otherMachineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, otherMachineId);

        var response = await _fixture.DismissAttemptDraftsAsync(tenant.CompanyId, DraftTestData.AttemptId);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        response.DismissedCount.Should().Be(expected: 2);
        machineDrafts.Select(draft => draft.State).Should().NotContain(DraftState.Pending);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAChat_DismissesOnlyThePendingDraftsOfThatChat()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherChatId = Guid.CreateVersion7();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var pendingId = await _fixture.AddChatMachineDraftAsync(tenant.CompanyId);

        var alreadyDismissedId = await _fixture.AddChatMachineDraftAsync(
            tenant.CompanyId,
            chatMessageId: Guid.CreateVersion7());

        await _fixture.DismissDraftAsync(tenant.CompanyId, alreadyDismissedId);

        await _fixture.AddChatMachineDraftAsync(
            tenant.CompanyId,
            chatId: otherChatId,
            chatMessageId: Guid.CreateVersion7());

        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);

        var response = await _fixture.DismissChatDraftsAsync(tenant.CompanyId, DraftTestData.ChatId);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        response.DismissedCount.Should().Be(expected: 1);
        machineDrafts.Single(draft => draft.Id == pendingId).State.Should().Be(DraftState.Dismissed);
        machineDrafts.Count(draft => draft.State == DraftState.Pending).Should().Be(expected: 2);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAnAttempt_LeavesAnotherAttemptAndAnotherCompanyPending()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(companyName: "Other company");
        var attemptId = Guid.CreateVersion7();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var otherCompanyMachineId = await _fixture.ArrangeMachineAsync(otherTenant.CompanyId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId, attemptId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);
        await _fixture.AddDraftsForMachineAsync(otherTenant.CompanyId, otherCompanyMachineId, attemptId);

        var response = await _fixture.DismissAttemptDraftsAsync(tenant.CompanyId, attemptId);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        var pendingCompanies = machineDrafts.Where(draft => draft.State == DraftState.Pending)
            .Select(draft => (draft.CompanyId, draft.Proposal.AttemptId))
            .ToList();

        response.DismissedCount.Should().Be(expected: 1);

        pendingCompanies.Should()
            .BeEquivalentTo(
                [(tenant.CompanyId, DraftTestData.AttemptId), (otherTenant.CompanyId, attemptId)]);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAnAttemptWithTwoCreateDraftsAndAnUpdateDraft_SendsTwoMessages()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var firstId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        var secondId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        await _fixture.AddMachineDraftAsync(tenant.CompanyId, machineId);

        await _fixture.DismissAttemptDraftsAsync(tenant.CompanyId, DraftTestData.AttemptId);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDraftDismissed>();

        messages.Select(message => message.DraftId).Should().BeEquivalentTo([firstId, secondId]);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAnAttemptWithoutPendingDrafts_ReturnsZero()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var response = await _fixture.DismissAttemptDraftsAsync(tenant.CompanyId, DraftTestData.AttemptId);

        response.DismissedCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task DismissMachineDrafts_WithoutPendingDrafts_ReturnsZero()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);

        var response = await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId);

        response.DismissedCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAMachineOfAnotherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(companyName: "Other company");
        var otherMachineId = await _fixture.ArrangeMachineAsync(otherTenant.CompanyId);

        var act = () => _fixture.DismissMachineDraftsAsync(tenant.CompanyId, otherMachineId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DismissMachineDrafts_InParallelWithItself_DismissesOnceAndTheOtherCallConflicts()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);
        var responses = new List<DismissMachineDraftsResponse>();
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(async () =>
                responses.Add(await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId))),
            race.RunAsync(async () =>
                responses.Add(await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId))));

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        race.SaveCounts.Should().Equal(1, 1);
        responses.Should().ContainSingle().Which.DismissedCount.Should().Be(expected: 1);

        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DraftErrors.DraftChanged);

        machineDrafts.Should().OnlyContain(draft => draft.State == DraftState.Dismissed);
    }
}
