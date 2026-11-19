using HealthyFac.MachineSimulator.Scenarios.Curves;

namespace HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

/// <summary>
/// The drained value on a curve. In the main phase the curve value replaces the phase value, and in the other running
/// phases the offset of the curve is added.
/// </summary>
public sealed record AbsoluteEffect : ModuleEffect
{
    /// <summary>The curve of the value along the drain.</summary>
    public required DrainCurve Curve { get; init; }
}
