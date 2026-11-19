using HealthyFac.MachineSimulator.Scenarios.Curves;

namespace HealthyFac.MachineSimulator.Scenarios.Modules;

/// <summary>
/// A status code that a draining module sends while its rule holds.
/// </summary>
public sealed record CodeRule
{
    /// <summary>The status code.</summary>
    public required string Code { get; init; }

    /// <summary>Tells whether the code is active at a drain progress.</summary>
    public required Func<double, bool> IsActive { get; init; }

    /// <summary>
    /// A code that starts at a progress and stays.
    /// </summary>
    /// <param name="code">The status code.</param>
    /// <param name="progress">The first progress with the code.</param>
    /// <returns>The rule.</returns>
    public static CodeRule From(string code, double progress) =>
        new() { Code = code, IsActive = current => current >= progress };

    /// <summary>
    /// A code while the drained value on a curve is above a limit.
    /// </summary>
    /// <param name="code">The status code.</param>
    /// <param name="curve">The curve of the value.</param>
    /// <param name="limit">The limit. The value must be above it.</param>
    /// <returns>The rule.</returns>
    public static CodeRule WhileAbove(string code, DrainCurve curve, double limit) =>
        new() { Code = code, IsActive = current => curve.ValueAt(current) > limit };

    /// <summary>
    /// A code while the drained value on a curve is below a limit.
    /// </summary>
    /// <param name="code">The status code.</param>
    /// <param name="curve">The curve of the value.</param>
    /// <param name="limit">The limit. The value must be below it.</param>
    /// <returns>The rule.</returns>
    public static CodeRule WhileBelow(string code, DrainCurve curve, double limit) =>
        new() { Code = code, IsActive = current => curve.ValueAt(current) < limit };
}
