namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The linear fall of a value while a part does not run, down to a floor. Keep the running value with
/// <see cref="SlowCooling.Hold"/>; the last value of each key lives in <see cref="ScenarioContext.Memory"/>.
/// </summary>
public static class SlowFall
{
    /// <summary>
    /// Lowers the last value of a key by the fall of the elapsed time. The value never goes below the floor.
    /// </summary>
    /// <param name="context">The context that holds the memory and the elapsed time.</param>
    /// <param name="key">The metric key.</param>
    /// <param name="fallback">The value to fall from when the key has no earlier value.</param>
    /// <param name="floor">The lowest value.</param>
    /// <param name="perSecond">The fall for each second.</param>
    /// <returns>The lowered value.</returns>
    public static double Fall(ScenarioContext context, string key, double fallback, double floor, double perSecond)
    {
        var last = context.Memory.TryGetValue(key, out var stored) ? stored : fallback;
        var next = Math.Max(floor, last - perSecond * context.Elapsed.TotalSeconds);
        context.Memory[key] = next;

        return next;
    }
}
