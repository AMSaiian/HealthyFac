using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Seeder.IntegrationTests.Cases.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Machines.Data;

namespace HealthyFac.Seeder.IntegrationTests.Cases;

[Collection(SeederCollection.Name)]
public sealed class CasesSeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealCasesFile_OpensTheCaseWithItsDraftedSignalOnTheLathe()
    {
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken);

        var (maintenanceCase, signal) = await _fixture.LoadOpenCaseAsync();
        var machines = await _fixture.LoadMachinesAsync();
        var lathe = machines.Single(machine => machine.Name == CasesTestData.LatheName);
        maintenanceCase.State.Should().Be(CaseState.Open);
        maintenanceCase.MachineId.Should().Be(lathe.Id);
        signal.CaseId.Should().Be(CasesTestData.CaseId);
        signal.MachineId.Should().Be(lathe.Id);
        signal.State.Should().Be(SignalState.Open);
        signal.Coverage.Should().Be(SignalCoverage.Drafted);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheRealCasesFile_StoresThePendingWorkOrderDraftForTheLathe()
    {
        await _fixture.RunFullSeedAsync(TestContext.Current.CancellationToken);

        var draft = await _fixture.LoadWorkOrderDraftAsync();
        var machines = await _fixture.LoadMachinesAsync();
        var lathe = machines.Single(machine => machine.Name == CasesTestData.LatheName);
        draft.Title.Should().Be(CasesTestData.DraftTitle);
        draft.State.Should().Be(DraftState.Pending);
        draft.MachineId.Should().Be(lathe.Id);
        draft.CaseId.Should().Be(CasesTestData.CaseId);
    }
}
