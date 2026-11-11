using HealthyFac.Core.Application.Drafts;
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
public sealed class DraftConcurrencyTests(CoreDatabaseFixture fixture)
{
    private const string FirstSerialNumber = "RACE-1";
    private const string SecondSerialNumber = "RACE-2";

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyDraft_InParallelWithDifferentSerialNumbers_AppliesOnce()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ApplyDraftAsync(tenant.CompanyId, draftId, name: "Race A", FirstSerialNumber)),
            race.RunAsync(() => _fixture.ApplyDraftAsync(
                tenant.CompanyId, draftId, name: "Race B", SecondSerialNumber)));

        var machineCount = await _fixture.CountMachinesAsync(tenant.CompanyId);
        var applied = await _fixture.ReadOutboxMessagesAsync<MachineDraftApplied>();

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DraftErrors.DraftChanged);
        machineCount.Should().Be(expected: 1);
        applied.Should().ContainSingle();
    }

    [Fact]
    public async Task DismissDraft_InParallelWithApply_ConflictsOnTheDraftRowVersion()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var draftId = await _fixture.AddMachineDraftAsync(tenant.CompanyId);
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ApplyDraftAsync(tenant.CompanyId, draftId, name: "Race A", FirstSerialNumber)),
            race.RunAsync(() => _fixture.DismissDraftAsync(tenant.CompanyId, draftId)));

        var drafts = await _fixture.LoadMachineDraftsAsync();

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(DraftErrors.DraftChanged);
        drafts.Should().ContainSingle().Which.State.Should().NotBe(DraftState.Pending);
    }
}
