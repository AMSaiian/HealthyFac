using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.OperatingPhases;

/// <summary>
/// The limits of one numeric metric inside one phase. A row replaces all four limits of the metric in that phase. An
/// empty value means no limit on that side. No row means the limits of the metric definition. The row is a child entity
/// of its phase: a soft delete of the phase soft deletes it, and a row that leaves the collection is soft deleted.
/// </summary>
public sealed class OperatingPhaseLimit : IEntity, IAuditable, ISoftDeletable
{
    private OperatingPhaseLimit()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid MetricDefinitionId { get; private set; }

    public double? WarningLow { get; private set; }

    public double? WarningHigh { get; private set; }

    public double? AlarmLow { get; private set; }

    public double? AlarmHigh { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>The four limits as one value.</summary>
    public MetricLimits Limits => new()
    {
        WarningLow = WarningLow,
        WarningHigh = WarningHigh,
        AlarmLow = AlarmLow,
        AlarmHigh = AlarmHigh,
    };

    /// <summary>
    /// Replaces the four limits of the row.
    /// </summary>
    /// <param name="limits">The new limits. The caller has checked them against the metric.</param>
    public void ChangeLimits(MetricLimits limits)
    {
        WarningLow = limits.WarningLow;
        WarningHigh = limits.WarningHigh;
        AlarmLow = limits.AlarmLow;
        AlarmHigh = limits.AlarmHigh;
    }

    /// <summary>
    /// Creates a limit row.
    /// </summary>
    /// <param name="metricDefinitionId">The numeric metric of the row.</param>
    /// <param name="valueKind">The value kind of the metric.</param>
    /// <param name="limits">The four limits.</param>
    /// <returns>The row.</returns>
    /// <exception cref="InvalidOperationException">The metric is not numeric, or the limits break a rule of
    /// <see cref="MetricLimits"/>.</exception>
    public static OperatingPhaseLimit Create(Guid metricDefinitionId, MetricValueKind valueKind, MetricLimits limits)
    {
        if (valueKind != MetricValueKind.Numeric)
        {
            throw OperatingPhaseInvariantErrors.LimitOnNonNumericMetric(valueKind);
        }

        limits.EnsureValid(valueKind);

        return new OperatingPhaseLimit
        {
            MetricDefinitionId = metricDefinitionId,
            WarningLow = limits.WarningLow,
            WarningHigh = limits.WarningHigh,
            AlarmLow = limits.AlarmLow,
            AlarmHigh = limits.AlarmHigh,
        };
    }
}
