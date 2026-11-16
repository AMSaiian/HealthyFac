using HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation;

[CollectionDefinition(Name)]
public sealed class SimulatorCollection : ICollectionFixture<SimulatorFixture>
{
    public const string Name = "Simulator";
}

/// <summary>
/// A collection of its own for the tests that stop the broker, so no other test sees a stopped container.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SimulatorOutageCollection : ICollectionFixture<SimulatorFixture>
{
    public const string Name = "SimulatorOutage";
}
