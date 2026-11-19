using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.OperatingPhases;

/// <summary>
/// One condition of an operating phase: a status or a metric, an operator, and its values. All conditions of a phase
/// must hold. The condition is a child entity of its phase: a soft delete of the phase soft deletes it, and a condition
/// that leaves the collection of the phase is soft deleted.
/// </summary>
public sealed class OperatingPhaseCondition : IEntity, IAuditable, ISoftDeletable
{
    private List<string>? _states;

    private OperatingPhaseCondition()
    {
        Operator = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid? MetricDefinitionId { get; private set; }

    public Guid? StatusDefinitionId { get; private set; }

    public PhaseConditionOperator Operator { get; private set; }

    /// <summary>The comparison value of a numeric operator, or the lower value of <c>between</c>.</summary>
    public double? Value { get; private set; }

    /// <summary>The upper value of <c>between</c>.</summary>
    public double? UpperValue { get; private set; }

    /// <summary>The state codes of <c>in</c>.</summary>
    public IReadOnlyList<string>? States => _states;

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Creates a condition and checks it against its source. Values that the operator does not use are dropped.
    /// </summary>
    /// <param name="source">The status or the metric that the condition reads.</param>
    /// <param name="conditionOperator">The operator. It must fit the source.</param>
    /// <param name="value">The comparison value of a numeric operator.</param>
    /// <param name="upperValue">The upper value of <c>between</c>.</param>
    /// <param name="states">The state codes of <c>in</c>. The metric must define each one.</param>
    /// <returns>The condition.</returns>
    /// <exception cref="InvalidOperationException">The operator does not fit the source, a needed value is missing,
    /// <c>between</c> has a lower value that is not below the upper value, or a state is not defined by the
    /// metric.</exception>
    public static OperatingPhaseCondition Create(
        PhaseConditionSource source,
        PhaseConditionOperator conditionOperator,
        double? value,
        double? upperValue,
        IReadOnlyList<string>? states
    )
    {
        if (!conditionOperator.Accepts(source))
        {
            throw OperatingPhaseInvariantErrors.OperatorDoesNotFit(conditionOperator.Value);
        }

        if (conditionOperator.HasComparisonValue && value is null)
        {
            throw OperatingPhaseInvariantErrors.ValueRequired(conditionOperator.Value);
        }

        if (conditionOperator == PhaseConditionOperator.Between)
        {
            if (upperValue is null)
            {
                throw OperatingPhaseInvariantErrors.UpperValueRequired(conditionOperator.Value);
            }

            if (value >= upperValue)
            {
                throw OperatingPhaseInvariantErrors.ValuesNotAscending(value!.Value, upperValue.Value);
            }
        }

        if (conditionOperator == PhaseConditionOperator.In)
        {
            EnsureStates(source, states);
        }

        return new OperatingPhaseCondition
        {
            MetricDefinitionId = source.MetricDefinitionId,
            StatusDefinitionId = source.StatusDefinitionId,
            Operator = conditionOperator,
            Value = conditionOperator.HasComparisonValue ? value : null,
            UpperValue = conditionOperator == PhaseConditionOperator.Between ? upperValue : null,
            _states = conditionOperator == PhaseConditionOperator.In ? states!.ToList() : null,
        };
    }

    /// <summary>
    /// Compares the condition with another one by value.
    /// </summary>
    /// <param name="other">The other condition.</param>
    /// <returns><see langword="true"/> when both read the same source with the same operator and values.</returns>
    public bool IsSameAs(OperatingPhaseCondition other) =>
        MetricDefinitionId == other.MetricDefinitionId
        && StatusDefinitionId == other.StatusDefinitionId
        && Operator == other.Operator
        && Value == other.Value
        && UpperValue == other.UpperValue
        && (_states ?? []).SequenceEqual(other._states ?? []);

    private static void EnsureStates(PhaseConditionSource source, IReadOnlyList<string>? states)
    {
        if (states is not { Count: > 0 })
        {
            throw OperatingPhaseInvariantErrors.StatesRequired();
        }

        var unknownState = states.FirstOrDefault(state => source.States?.Contains(state) != true);

        if (unknownState is not null)
        {
            throw OperatingPhaseInvariantErrors.UnknownState(unknownState);
        }
    }
}
