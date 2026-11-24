using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// The scenario of every other machine: it builds the values from the metric definitions. A number starts in the middle
/// of its range and moves by at most 1 % of the range for each message. A boolean is <see langword="false"/>, and a
/// state set shows its first state. The scenario has no modules, no counters, and no status codes.
/// </summary>
public sealed class GenericScenario : IMachineScenario
{
    /// <summary>The largest move of a number for each message, as a share of its range.</summary>
    public const double MaxStepShare = 0.01;

    public ScenarioKind Kind => ScenarioKind.Generic;

    public IReadOnlyList<ModuleDefinition> Modules { get; } = [];

    public IReadOnlyDictionary<string, double> CounterStartValues { get; } = new Dictionary<string, double>();

    /// <summary>
    /// Gives the range that a number moves in. With only a maximum, the lower edge is the maximum minus its absolute
    /// value. With only a minimum, the upper edge is the minimum plus its absolute value. Without a bound, the range
    /// is the fixed value 0.
    /// </summary>
    /// <param name="min">The lower bound, or <see langword="null"/>.</param>
    /// <param name="max">The upper bound, or <see langword="null"/>.</param>
    /// <returns>The lower and the upper edge. A range of width 0 is fixed at its edge.</returns>
    public static (double Low, double High) RangeOf(double? min, double? max) =>
        (min, max) switch
        {
            ({ } low, { } high) => (low, high),
            (null, { } high) => (high - Math.Abs(high), high),
            ({ } low, null) => (low, low + Math.Abs(low)),
            _ => (0, 0),
        };

    public ScenarioSample Sample(ScenarioContext context)
    {
        var values = new Dictionary<string, object>();

        foreach (var metric in context.Definitions.Metrics)
        {
            values[metric.Key] = ValueOf(metric, context);
        }

        return new ScenarioSample { Values = values, StatusCodes = [], IsRunning = true };
    }

    private static object ValueOf(MetricInfo metric, ScenarioContext context)
    {
        if (metric.ValueKind == MetricValueKind.Boolean)
        {
            return false;
        }

        if (metric.ValueKind == MetricValueKind.StateSet)
        {
            return metric.States?.FirstOrDefault() ?? string.Empty;
        }

        return NextNumber(metric, context);
    }

    private static double NextNumber(MetricInfo metric, ScenarioContext context)
    {
        var (low, high) = RangeOf(metric.Min, metric.Max);
        var width = high - low;
        var next = context.Memory.TryGetValue(metric.Key, out var last)
            ? last + (context.Random.NextDouble() * 2 - 1) * MaxStepShare * width
            : low + width / 2;
        var clamped = Math.Clamp(next, low, high);
        context.Memory[metric.Key] = clamped;

        return clamped;
    }
}
