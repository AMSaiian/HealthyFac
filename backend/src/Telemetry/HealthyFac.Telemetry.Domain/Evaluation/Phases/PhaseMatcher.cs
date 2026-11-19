using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.Domain.Evaluation.Phases;

/// <summary>
/// Finds the operating phase that a sample matches.
/// </summary>
public static class PhaseMatcher
{
    /// <summary>
    /// Returns the first phase by priority whose conditions all hold.
    /// </summary>
    /// <param name="phases">The active phases of the machine.</param>
    /// <param name="values">The values of the sample time.</param>
    /// <returns>The ID of the phase, or <see langword="null"/> when none holds or there are no phases.</returns>
    public static Guid? Match(IEnumerable<OperatingPhase> phases, SampleValues values) =>
        phases
            .OrderBy(phase => phase.Priority)
            .FirstOrDefault(phase => phase.Conditions.All(condition => Holds(condition, values)))
            ?.Id;

    private static bool Holds(OperatingPhaseCondition condition, SampleValues values)
    {
        var op = condition.Operator;

        if (condition.StatusDefinitionId is { } statusId)
        {
            var isActive = values.ActiveStatusIds.Contains(statusId);

            return op == PhaseConditionOperator.Active ? isActive : !isActive;
        }

        var metricId = condition.MetricDefinitionId!.Value;

        if (op == PhaseConditionOperator.IsTrue || op == PhaseConditionOperator.IsFalse)
        {
            var expected = op == PhaseConditionOperator.IsTrue ? bool.TrueString : bool.FalseString;

            return values.Discrete.TryGetValue(metricId, out var flag)
                && string.Equals(flag, expected, StringComparison.OrdinalIgnoreCase);
        }

        if (op == PhaseConditionOperator.In)
        {
            return values.Discrete.TryGetValue(metricId, out var state) && condition.States!.Contains(state);
        }

        return values.Numeric.TryGetValue(metricId, out var number) && CompareNumber(condition, number);
    }

    private static bool CompareNumber(OperatingPhaseCondition condition, double number)
    {
        var op = condition.Operator;

        if (op == PhaseConditionOperator.GreaterThan)
        {
            return number > condition.Value;
        }

        if (op == PhaseConditionOperator.GreaterOrEqual)
        {
            return number >= condition.Value;
        }

        if (op == PhaseConditionOperator.LessThan)
        {
            return number < condition.Value;
        }

        if (op == PhaseConditionOperator.LessOrEqual)
        {
            return number <= condition.Value;
        }

        return number >= condition.Value && number <= condition.UpperValue;
    }
}
