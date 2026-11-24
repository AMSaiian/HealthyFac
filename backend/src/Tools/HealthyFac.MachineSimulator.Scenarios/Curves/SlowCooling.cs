namespace HealthyFac.MachineSimulator.Scenarios.Curves;

/// <summary>
/// The slow fall of a temperature toward the ambient temperature while a part does not run. The last value of each key
/// lives in <see cref="ScenarioContext.Memory"/>.
/// </summary>
public static class SlowCooling
{
    /// <summary>The fall in degrees Celsius for each second.</summary>
    public const double DegreesPerSecond = 0.05;

    /// <summary>The ambient temperature in degrees Celsius.</summary>
    public const double Ambient = 22;

    /// <summary>
    /// Keeps the value of a running part, so a later stop cools from it.
    /// </summary>
    /// <param name="context">The context that holds the memory.</param>
    /// <param name="key">The metric key.</param>
    /// <param name="value">The running value.</param>
    /// <returns>The same value.</returns>
    public static double Hold(ScenarioContext context, string key, double value)
    {
        context.Memory[key] = value;

        return value;
    }

    /// <summary>
    /// Lowers the last value of a key by the cooling of the elapsed time. The value never falls below the ambient
    /// temperature.
    /// </summary>
    /// <param name="context">The context that holds the memory and the elapsed time.</param>
    /// <param name="key">The metric key.</param>
    /// <param name="fallback">The value to cool from when the key has no earlier value.</param>
    /// <returns>The cooled value.</returns>
    public static double Cool(ScenarioContext context, string key, double fallback)
    {
        var last = context.Memory.TryGetValue(key, out var stored) ? stored : fallback;
        var next = Math.Max(Ambient, last - DegreesPerSecond * context.Elapsed.TotalSeconds);
        context.Memory[key] = next;

        return next;
    }
}
