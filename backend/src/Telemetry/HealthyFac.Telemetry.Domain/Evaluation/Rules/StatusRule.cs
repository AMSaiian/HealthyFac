using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// Maps an active status to a severity. A warning status opens a warning and an error status an alarm, at once.
/// </summary>
public static class StatusRule
{
    /// <summary>
    /// Builds the conditions of one sample.
    /// </summary>
    /// <param name="severity">The severity of the status definition.</param>
    /// <param name="isActive">Tells whether the status is active at the sample.</param>
    /// <returns>The conditions. A status of the severity info opens nothing and counts as inactive.</returns>
    public static RuleCondition Evaluate(StatusSeverity severity, bool isActive)
    {
        var isWarning = isActive && severity == StatusSeverity.Warning;
        var isError = isActive && severity == StatusSeverity.Error;

        return new RuleCondition
        {
            IsOpen = isWarning || isError,
            IsAlarm = isError,
            IsResolved = !(isWarning || isError),
        };
    }
}
