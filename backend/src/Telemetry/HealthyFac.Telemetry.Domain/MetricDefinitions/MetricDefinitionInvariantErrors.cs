namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// Defines the exceptions that a metric definition throws when a caller breaks an invariant. The Application
/// validator checks the same rules first, so these exceptions mark a programming error.
/// </summary>
public static class MetricDefinitionInvariantErrors
{
    public static InvalidOperationException InvalidKey(string key) =>
        new($"'{key}' is not a valid metric key.");

    public static InvalidOperationException UnitDoesNotFit(
        MetricValueKind kind,
        string quantityKind,
        string unitName
    ) =>
        new($"The unit '{quantityKind}.{unitName}' does not fit a metric of the kind '{kind.Value}'.");

    public static InvalidOperationException RangeOnlyForNumeric(MetricValueKind kind) =>
        new($"A metric of the kind '{kind.Value}' has no range.");

    public static InvalidOperationException RangeNotAscending(double min, double max) =>
        new($"The minimum '{min}' must be below the maximum '{max}'.");

    public static InvalidOperationException StatesOnlyForStateSet(MetricValueKind kind) =>
        new($"A metric of the kind '{kind.Value}' has no states.");

    public static InvalidOperationException StateSetNeedsStates(int minCount) =>
        new($"A state set needs at least {minCount} unique states.");

    public static InvalidOperationException InvalidState(string state) =>
        new($"'{state}' is not a valid state code.");
}
