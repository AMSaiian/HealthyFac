using HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation;

[Collection(SimulatorCollection.Name)]
[Feature(TestFeatures.Simulation)]
public sealed class DiscoveryTests(SimulatorFixture fixture)
{
    private readonly SimulatorFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(SimulatorTestData.DemoMachines), MemberType = typeof(SimulatorTestData))]
    public async Task Discover_WithDemoSerialNumber_BindsTheScenario(string serialNumber, ScenarioKind expected)
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(serialNumber);

        var view = _fixture.Engine.Find(machine.MachineId, _fixture.Clock.GetUtcNow().UtcDateTime);

        view!.Scenario.Should().Be(expected);
    }

    [Fact]
    public async Task Discover_WithUnknownSerialNumber_BindsTheGenericScenario()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);

        var view = _fixture.Engine.Find(machine.MachineId, _fixture.Clock.GetUtcNow().UtcDateTime);

        view!.Scenario.Should().Be(ScenarioKind.Generic);
    }

    [Fact]
    public async Task Tick_WithRunningMachine_PublishesAMessage()
    {
        await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);

        var messages = await _fixture.TickAsync();

        messages.Should().ContainSingle();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public async Task Tick_AfterTheMachineIsDeleted_PublishesNothingAfterTheNextStatePoll()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);
        await _fixture.DeleteMachineAsync(machine);
        await _fixture.PollStateAsync();

        var messages = await _fixture.TickExpectingNothingAsync();

        messages.Should().BeEmpty();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public async Task Tick_AfterTheRegistrationIsDeleted_PublishesNothingAfterTheNextStatePoll()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);
        await _fixture.UnregisterMachineAsync(machine);
        await _fixture.PollStateAsync();

        var messages = await _fixture.TickExpectingNothingAsync();

        messages.Should().BeEmpty();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public async Task Tick_WithMachineWithoutRegistration_PublishesNothingUntilTheRegistrationExists()
    {
        var companyId = await _fixture.CreateCompanyAsync();
        var machine = await _fixture.CreateMachineAsync(companyId, SimulatorTestData.GenericSerialNumber);
        await _fixture.DiscoverAsync();

        var beforeRegistration = await _fixture.TickExpectingNothingAsync();
        await _fixture.RegisterMachineAsync(machine);
        await _fixture.PollStateAsync();
        var afterRegistration = await _fixture.TickAsync();

        beforeRegistration.Should().BeEmpty();
        afterRegistration.Should().ContainSingle();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public async Task Tick_WithGoLiveDateInTheFuture_PublishesNothing()
    {
        var companyId = await _fixture.CreateCompanyAsync();

        var machine = await _fixture.CreateMachineAsync(
            companyId,
            SimulatorTestData.GenericSerialNumber,
            goLiveInDays: 2);

        await _fixture.RegisterMachineAsync(machine);
        await _fixture.DiscoverAsync();

        var messages = await _fixture.TickExpectingNothingAsync();

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Discover_WithStoredCounterReading_StartsTheCounterThere()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(
            ScenarioBinding.CncSerialNumber,
            isDiscovered: false);

        await _fixture.StoreReadingAsync(
            machine.MachineId,
            SimulatorTestData.CounterKey,
            SimulatorTestData.StoredCounterValue);

        await _fixture.DiscoverAsync();

        var view = _fixture.Engine.Find(machine.MachineId, _fixture.Clock.GetUtcNow().UtcDateTime);

        view!.Counters[SimulatorTestData.CounterKey].Should().Be(SimulatorTestData.StoredCounterValue);
    }

    [Fact]
    public async Task Discover_WithoutStoredCounterReading_StartsTheCounterAtItsStartValue()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(ScenarioBinding.CncSerialNumber);

        var view = _fixture.Engine.Find(machine.MachineId, _fixture.Clock.GetUtcNow().UtcDateTime);

        view!.Counters[SimulatorTestData.CounterKey].Should().Be(expected: 0);
    }

    [Fact]
    public async Task WriteThroughSimulatorConnection_WithReadOnlyOption_FailsWithReadOnlyState()
    {
        var act = async () => await _fixture.WriteThroughSimulatorConnectionAsync();

        var exception = await act.Should().ThrowAsync<Npgsql.PostgresException>();
        exception.Which.SqlState.Should().Be(SimulatorTestData.ReadOnlySqlState);
    }
}
