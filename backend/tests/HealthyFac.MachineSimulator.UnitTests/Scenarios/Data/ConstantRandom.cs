namespace HealthyFac.MachineSimulator.UnitTests.Scenarios.Data;

/// <summary>
/// A random source that always returns the same value, so a test can push a walk to its edge.
/// </summary>
/// <param name="value">The value of every <see cref="NextDouble"/> call, from 0 to 1.</param>
public sealed class ConstantRandom(double value) : Random
{
    private readonly double _value = value;

    public override double NextDouble() => _value;
}
