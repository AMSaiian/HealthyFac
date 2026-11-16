namespace HealthyFac.MachineSimulator.Machines;

/// <summary>
/// The changes that a snapshot caused in a machine. The engine logs them.
/// </summary>
[Flags]
public enum SnapshotChange
{
    None = 0,
    Rebound = 1,
    Reset = 2,
}
