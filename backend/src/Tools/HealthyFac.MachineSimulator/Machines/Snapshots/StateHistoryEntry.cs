using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.MachineSimulator.Machines.Snapshots;

/// <summary>
/// One row of the Core state history of a machine.
/// </summary>
public sealed record StateHistoryEntry
{
    /// <summary>The state.</summary>
    public required MachineState State { get; init; }

    /// <summary>The UTC instant that the state started.</summary>
    public required DateTime StartedAt { get; init; }
}
