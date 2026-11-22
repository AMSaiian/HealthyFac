namespace HealthyFac.Seeder.Machines;

/// <summary>The content of <c>machines.json</c>: the applied machines and the state changes of the machines.</summary>
public sealed class MachinesFile
{
    public required IReadOnlyList<MachineSeed> Machines { get; init; }

    public required IReadOnlyList<MachineStateSeed> StateChanges { get; init; }
}
