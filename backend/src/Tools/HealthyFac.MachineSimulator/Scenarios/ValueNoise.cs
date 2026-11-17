namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// Adds a small random noise to a numeric value. Counters and booleans get none.
/// </summary>
public static class ValueNoise
{
    /// <summary>The largest share of the value that the noise adds or removes.</summary>
    public const double MaxShare = 0.005;

    /// <summary>
    /// Adds the noise.
    /// </summary>
    /// <param name="value">The clean value.</param>
    /// <param name="random">The random source.</param>
    /// <returns>The value within plus or minus 0.5 % of <paramref name="value"/>.</returns>
    public static double Apply(double value, Random random) =>
        value * (1 + (random.NextDouble() * 2 - 1) * MaxShare);
}
