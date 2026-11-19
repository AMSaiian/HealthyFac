namespace HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

/// <summary>
/// An amount that grows linearly from 0 to <paramref name="AtEnd"/> along the drain and is added to the value.
/// </summary>
public sealed record OffsetEffect : ModuleEffect
{
    /// <summary>The amount at the trip point.</summary>
    public required double AtEnd { get; init; }
}
