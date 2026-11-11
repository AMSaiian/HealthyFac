using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Drafts;

[Collection(CoreMachinesCollection.Name)]
public sealed class DismissMachineDraftsTests(CoreDatabaseFixture fixture)
{
    private const int PendingDraftsOfAMachine = 3;

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DismissMachineDrafts_WithPendingDraftsOfAllKinds_DismissesThemAndKeepsTheOthers()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        var otherMachineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, otherMachineId);

        var response = await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metricDrafts = await _fixture.LoadMetricDraftsAsync();
        var statusDrafts = await _fixture.LoadStatusDraftsAsync();
        response.DismissedCount.Should().Be(PendingDraftsOfAMachine);
        machineDrafts.Single(draft => draft.TargetMachineId == machineId).State.Should().Be(DraftState.Dismissed);
        statusDrafts.Single(draft => draft.TargetMachineId == machineId).State.Should().Be(DraftState.Dismissed);
        metricDrafts.Where(draft => draft.TargetMachineId == machineId)
            .Select(draft => draft.State)
            .Should()
            .BeEquivalentTo([DraftState.Dismissed, DraftState.Applied]);
        machineDrafts.Single(draft => draft.TargetMachineId == otherMachineId).State.Should().Be(DraftState.Pending);
        statusDrafts.Single(draft => draft.TargetMachineId == otherMachineId).State.Should().Be(DraftState.Pending);
        metricDrafts.Where(draft => draft.TargetMachineId == otherMachineId)
            .Select(draft => draft.State)
            .Should()
            .BeEquivalentTo([DraftState.Pending, DraftState.Applied]);
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
            race.RunAsync(
                async () => responses.Add(await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId))),
            race.RunAsync(
                async () => responses.Add(await _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId))));

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metricDrafts = await _fixture.LoadMetricDraftsAsync();
        var statusDrafts = await _fixture.LoadStatusDraftsAsync();
        race.SaveCounts.Should().Equal(1, 1);
        responses.Should().ContainSingle().Which.DismissedCount.Should().Be(PendingDraftsOfAMachine);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DraftErrors.DraftChanged);
        machineDrafts.Should().OnlyContain(draft => draft.State == DraftState.Dismissed);
        statusDrafts.Should().OnlyContain(draft => draft.State == DraftState.Dismissed);
        metricDrafts.Select(draft => draft.State).Should().BeEquivalentTo([DraftState.Dismissed, DraftState.Applied]);
    }

    [Fact]
    public async Task DismissMachineDrafts_InParallelWithMarkAppliedOfOneDraft_ConflictsAndChangesNothing()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.ArrangeMachineAsync(tenant.CompanyId);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);
        var pendingMetric = (await _fixture.LoadMetricDraftsAsync()).Single(draft => draft.State == DraftState.Pending);
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.MarkDraftsAppliedAsync(tenant.CompanyId, pendingMetric.Id)),
            race.RunAsync(() => _fixture.DismissMachineDraftsAsync(tenant.CompanyId, machineId)));

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metricDrafts = await _fixture.LoadMetricDraftsAsync();
        var statusDrafts = await _fixture.LoadStatusDraftsAsync();
        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DraftErrors.DraftChanged);
        var dismissedCount = machineDrafts.Count(draft => draft.State == DraftState.Dismissed)
            + metricDrafts.Count(draft => draft.State == DraftState.Dismissed)
            + statusDrafts.Count(draft => draft.State == DraftState.Dismissed);
        var isDismissWinner = failures[1] is null;
        dismissedCount.Should().Be(isDismissWinner ? PendingDraftsOfAMachine : 0);
        metricDrafts.Count(draft => draft.State == DraftState.Applied).Should().Be(isDismissWinner ? 1 : 2);
    }
}
