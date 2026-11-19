namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// A change that a draining module makes to one metric key.
/// </summary>
public abstract record ModuleEffect
{
    /// <summary>The metric key.</summary>
    public required string Key { get; init; }
}
