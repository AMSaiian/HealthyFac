namespace HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

/// <summary>
/// A share that grows linearly from 0 to <paramref name="ShareAtEnd"/> along the drain and scales the value.
/// </summary>
public sealed record RelativeEffect : ModuleEffect
{
    /// <summary>The share at the trip point (0.2 means plus 20 %).</summary>
    public required double ShareAtEnd { get; init; }
}
