using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Ingest.Data;
using HealthyFac.Seeder.IntegrationTests.Machines.Data;
using HealthyFac.Seeder.IntegrationTests.People.Data;

namespace HealthyFac.Seeder.IntegrationTests.Machines;

[Collection(SeederCollection.Name)]
public sealed class MachinesSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealMachinesFile_CreatesTheThreeMachinesWithTheirStatesAndDrafts()
    {
        await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);

        var machines = await _fixture.LoadMachinesAsync();
        var drafts = await _fixture.LoadMachineDraftsAsync();

        var goLiveOn = DateOnly.FromDateTime(
            PeopleTestData.Anchor.UtcDateTime.AddDays(-MachinesTestData.GoLiveDaysBefore));

        machines.Should().HaveCount(MachinesTestData.MachineCount);
        machines.Should().OnlyContain(machine => machine.ExpectedGoLiveOn == goLiveOn);
        var compressor = machines.Single(machine => machine.Name == MachinesTestData.CompressorName);

        compressor.StateHistory.OrderBy(history => history.StartedAt)
            .Select(history => history.State)
            .Should()
            .Equal(MachinesTestData.CompressorStates);

        drafts.Count(draft => draft.State == DraftState.Applied).Should().Be(MachinesTestData.MachineCount);
        drafts.Count(draft => draft.State == DraftState.Pending).Should().Be(MachinesTestData.MachineCount);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealMachinesFile_AppliesEveryDefinitionOfSetOneAndLeavesSetTwoPending()
    {
        await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);

        var definitions = await _fixture.CountTelemetryDefinitionsAsync();
        var pending = await _fixture.CountPendingDefinitionDraftsAsync();
        definitions.Registrations.Should().Be(MachinesTestData.MachineCount);
        definitions.Metrics.Should().Be(MachinesTestData.MetricCountOfSet);
        definitions.Coolants.Should().Be(expected: 1);
        definitions.Statuses.Should().Be(MachinesTestData.StatusCountOfSet);
        definitions.Phases.Should().Be(MachinesTestData.PhaseCountOfSet);
        pending.Metrics.Should().Be(MachinesTestData.MetricCountOfSet);
        pending.Statuses.Should().Be(MachinesTestData.StatusCountOfSet);
        pending.Phases.Should().Be(MachinesTestData.PhaseCountOfSet);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealMachinesFile_TiesTheFilesOfSetOneAndLeavesTheFilesOfSetTwoUntied()
    {
        await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);

        var files = await _fixture.LoadTiedFilesAsync();
        var machines = await _fixture.LoadMachinesAsync();
        files.Count(file => file.MachineId is not null).Should().Be(MachinesTestData.MachineCount);

        files.Where(file => file.MachineId is not null)
            .Select(file => file.MachineId!.Value)
            .Should()
            .BeEquivalentTo(machines.Select(machine => machine.Id));

        files.Count(file => file.MachineId is null).Should().Be(MachinesTestData.MachineCount);
    }

    [Fact(Explicit = true)]
    public async Task ApplyMachineDraft_OfTheSetTwoDraftsWithoutEdits_CreatesMachinesFourToSix()
    {
        var context = await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);

        await _fixture.ApplyPendingMachineDraftsAsync(context, TestContext.Current.CancellationToken);

        var machines = await _fixture.LoadMachinesAsync();
        var isCoolantPending = await _fixture.IsCoolantDraftPendingWithoutKindAsync();
        machines.Should().HaveCount(MachinesTestData.SecondSetMachineCount);
        isCoolantPending.Should().BeTrue();
    }
}
