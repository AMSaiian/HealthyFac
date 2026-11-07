using System.Diagnostics.CodeAnalysis;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.MetricUnits;

namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// Represents a metric that a machine reports: a key, a value kind, a unit, and for a number a range. Every kind has
/// a unit: a number a physical one, a boolean and a state set a discrete one. The key, the value kind, and the unit
/// never change. A removal keeps the row (soft delete).
/// </summary>
public class MetricDefinition : IEntity, IAuditable, ISoftDeletable
{
    private List<string>? _states;

    /// <summary>
    /// Creates a metric definition.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine. It never changes.</param>
    /// <param name="key">The key in the payload. It never changes.</param>
    /// <param name="name">The name as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="valueKind">The kind of the value. It never changes.</param>
    /// <param name="measurementUnit">The unit, with its quantity kind loaded. It never changes.</param>
    /// <param name="min">The lower bound of a numeric metric, or <see langword="null"/>.</param>
    /// <param name="max">The upper bound of a numeric metric, or <see langword="null"/>.</param>
    /// <param name="states">The state codes of a state set, otherwise <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">The key is not valid, or the fields do not fit the value
    /// kind.</exception>
    public MetricDefinition(
        Guid machineRegistrationId,
        string key,
        string name,
        string? description,
        MetricValueKind valueKind,
        MeasurementUnit measurementUnit,
        double? min,
        double? max,
        IReadOnlyList<string>? states
    )
    {
        if (!MetricDefinitionRules.IsValidKey(key))
        {
            throw MetricDefinitionInvariantErrors.InvalidKey(key);
        }

        if (!DiscreteUnits.Fits(valueKind, measurementUnit))
        {
            throw MetricDefinitionInvariantErrors.UnitDoesNotFit(
                valueKind, measurementUnit.QuantityKind.Name, measurementUnit.Name);
        }

        MachineRegistrationId = machineRegistrationId;
        Key = key;
        ValueKind = valueKind;
        MeasurementUnitId = measurementUnit.Id;
        Name = name;
        Description = description;
        ApplyValues(min, max, states);
    }

    /// <summary>
    /// The constructor that EF Core binds when it loads a row. A stored row already passed the rules of the public
    /// constructor, so this one assigns the values only.
    /// </summary>
    private MetricDefinition(
        Guid machineRegistrationId,
        string key,
        string name,
        string? description,
        MetricValueKind valueKind,
        Guid measurementUnitId,
        double? min,
        double? max,
        IReadOnlyList<string>? states
    )
    {
        MachineRegistrationId = machineRegistrationId;
        Key = key;
        Name = name;
        Description = description;
        ValueKind = valueKind;
        MeasurementUnitId = measurementUnitId;
        Min = min;
        Max = max;
        _states = states?.ToList();
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid MachineRegistrationId { get; private set; }
    public string Key { get; private set; }
    public string Name { get; private set; }

    /// <summary>The description as HTML. The Application layer checks it against the rich text policy.</summary>
    public string? Description { get; private set; }

    public MetricValueKind ValueKind { get; private set; }
    public Guid MeasurementUnitId { get; private set; }
    public MeasurementUnit MeasurementUnit { get; private set; } = null!;

    /// <summary>The lower bound of a numeric metric. The sender may still send a lower value.</summary>
    public double? Min { get; private set; }

    /// <summary>The upper bound of a numeric metric. The sender may still send a higher value.</summary>
    public double? Max { get; private set; }

    /// <summary>The state codes of a state set. A change affects only the readings received after it.</summary>
    public IReadOnlyList<string>? States => _states;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the data that a user edits. The key, the value kind, and the unit never change.
    /// </summary>
    /// <param name="name">The name as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="min">The lower bound of a numeric metric, or <see langword="null"/>.</param>
    /// <param name="max">The upper bound of a numeric metric, or <see langword="null"/>.</param>
    /// <param name="states">The state codes of a state set, otherwise <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">The fields do not fit the value kind.</exception>
    [MemberNotNull(nameof(Name))]
    public void Update(string name, string? description, double? min, double? max, IReadOnlyList<string>? states)
    {
        Name = name;
        Description = description;
        ApplyValues(min, max, states);
    }

    private void ApplyValues(double? min, double? max, IReadOnlyList<string>? states)
    {
        if (ValueKind != MetricValueKind.Numeric && (min is not null || max is not null))
        {
            throw MetricDefinitionInvariantErrors.RangeOnlyForNumeric(ValueKind);
        }

        if (min is { } lower && max is { } upper && lower >= upper)
        {
            throw MetricDefinitionInvariantErrors.RangeNotAscending(lower, upper);
        }

        if (ValueKind != MetricValueKind.StateSet && states is not null)
        {
            throw MetricDefinitionInvariantErrors.StatesOnlyForStateSet(ValueKind);
        }

        if (ValueKind == MetricValueKind.StateSet)
        {
            EnsureStates(states);
        }

        Min = min;
        Max = max;
        _states = states?.ToList();
    }

    private static void EnsureStates([NotNull] IReadOnlyList<string>? states)
    {
        if (states is null
            || states.Count < MetricDefinitionRules.MinStateCount
            || states.Distinct(StringComparer.Ordinal).Count() != states.Count)
        {
            throw MetricDefinitionInvariantErrors.StateSetNeedsStates(MetricDefinitionRules.MinStateCount);
        }

        var invalidState = states.FirstOrDefault(state => !MetricDefinitionRules.IsValidKey(state));

        if (invalidState is not null)
        {
            throw MetricDefinitionInvariantErrors.InvalidState(invalidState);
        }
    }
}
