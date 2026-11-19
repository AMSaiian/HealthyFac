namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// A boolean metric that turns to a value when the drain reaches a progress.
/// </summary>
public sealed record SwitchEffect : ModuleEffect
{
    /// <summary>The value after the switch.</summary>
    public required bool IsOn { get; init; }

    /// <summary>The progress at which the value switches.</summary>
    public required double FromProgress { get; init; }
}
