namespace HealthyFac.MachineSimulator.Control;

/// <summary>
/// Starts or stops the messages of one machine.
/// </summary>
public sealed record SetRunRequest
{
    public bool? IsRunning { get; init; }
}
