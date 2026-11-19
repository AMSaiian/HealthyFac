using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.OperatingPhases;

/// <summary>
/// Describes what a phase condition reads: a status definition, or a metric definition with its value kind and its
/// state codes. The caller builds it from the stored definitions of the machine, so a condition never needs to load
/// them.
/// </summary>
public sealed record PhaseConditionSource
{
    private PhaseConditionSource()
    {
    }

    public Guid? MetricDefinitionId { get; private init; }

    public Guid? StatusDefinitionId { get; private init; }

    /// <summary>The value kind of the metric, or <see langword="null"/> for a status.</summary>
    public MetricValueKind? ValueKind { get; private init; }

    /// <summary>The state codes of a state set metric, otherwise <see langword="null"/>.</summary>
    public IReadOnlyList<string>? States { get; private init; }

    /// <summary>
    /// Creates the source of a status definition.
    /// </summary>
    /// <param name="statusDefinitionId">The ID of the status definition.</param>
    /// <returns>The source.</returns>
    public static PhaseConditionSource Status(Guid statusDefinitionId) =>
        new() { StatusDefinitionId = statusDefinitionId };

    /// <summary>
    /// Creates the source of a metric definition.
    /// </summary>
    /// <param name="metricDefinitionId">The ID of the metric definition.</param>
    /// <param name="valueKind">The value kind of the metric.</param>
    /// <param name="states">The state codes of a state set metric, otherwise <see langword="null"/>.</param>
    /// <returns>The source.</returns>
    public static PhaseConditionSource Metric(
        Guid metricDefinitionId,
        MetricValueKind valueKind,
        IReadOnlyList<string>? states
    ) =>
        new() { MetricDefinitionId = metricDefinitionId, ValueKind = valueKind, States = states };
}
