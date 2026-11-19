namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The points of a drain that the simulator logs: the normal limit, the alarm, and the trip.
/// </summary>
public enum DrainMilestone
{
    NormalLimit,
    Alarm,
    Trip,
}
