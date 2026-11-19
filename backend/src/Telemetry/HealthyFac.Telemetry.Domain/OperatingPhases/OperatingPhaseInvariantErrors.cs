using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.OperatingPhases;

/// <summary>
/// Defines the exceptions that an operating phase throws when a caller breaks an invariant. The Application
/// validator checks the same rules first, so these exceptions mark a programming error.
/// </summary>
public static class OperatingPhaseInvariantErrors
{
    public static InvalidOperationException InvalidCode(string code) =>
        new($"'{code}' is not a valid phase code.");

    public static InvalidOperationException NegativePriority(int priority) =>
        new($"The priority '{priority}' must be at least 0.");

    public static InvalidOperationException ConditionsRequired() =>
        new(message: "A phase needs at least 1 condition.");

    public static InvalidOperationException OperatorDoesNotFit(string operatorValue) =>
        new($"The operator '{operatorValue}' does not fit the source of the condition.");

    public static InvalidOperationException ValueRequired(string operatorValue) =>
        new($"The operator '{operatorValue}' needs a value.");

    public static InvalidOperationException UpperValueRequired(string operatorValue) =>
        new($"The operator '{operatorValue}' needs an upper value.");

    public static InvalidOperationException ValuesNotAscending(double value, double upperValue) =>
        new($"The value '{value}' must be below the upper value '{upperValue}'.");

    public static InvalidOperationException StatesRequired() =>
        new(message: "The operator 'in' needs at least 1 state.");

    public static InvalidOperationException UnknownState(string state) =>
        new($"The metric does not define the state '{state}'.");

    public static InvalidOperationException LimitOnNonNumericMetric(MetricValueKind kind) =>
        new($"A phase limit needs a numeric metric, not '{kind.Value}'.");

    public static InvalidOperationException DuplicateLimit(Guid metricDefinitionId) =>
        new($"The phase has more than 1 limit row for the metric '{metricDefinitionId}'.");

    public static InvalidOperationException LimitOnConditionMetric(Guid metricDefinitionId) =>
        new($"The phase has a limit row for the metric '{metricDefinitionId}' that its own condition uses.");
}
