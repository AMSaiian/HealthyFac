using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.MachineSimulator.Scenarios.Machines;

/// <summary>
/// One metric definition of a machine, as the last state poll read it.
/// </summary>
public sealed record MetricInfo
{
    /// <summary>The key in the payload.</summary>
    public required string Key { get; init; }

    /// <summary>The kind of the value.</summary>
    public required MetricValueKind ValueKind { get; init; }

    /// <summary>The lower bound of a numeric metric, or <see langword="null"/>.</summary>
    public double? Min { get; init; }

    /// <summary>The upper bound of a numeric metric, or <see langword="null"/>.</summary>
    public double? Max { get; init; }

    /// <summary>The state codes of a state set, or <see langword="null"/>.</summary>
    public IReadOnlyList<string>? States { get; init; }
}
