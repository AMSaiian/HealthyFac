using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.MetricUnits;

/// <summary>
/// Names the own quantity kind of the units that a boolean metric and a state set use. A person never picks these
/// units: the system sets them, and the catalog list hides them.
/// </summary>
public static class DiscreteUnits
{
    public const string KindName = "Discrete";
    public const string BooleanName = "Boolean";
    public const string StateName = "State";

    /// <summary>
    /// Tells whether a unit belongs to the discrete kind.
    /// </summary>
    /// <param name="unit">The unit, with its quantity kind loaded.</param>
    /// <returns><see langword="true"/> for a discrete unit.</returns>
    public static bool IsDiscrete(MeasurementUnit unit) => unit.QuantityKind.Name == KindName;

    /// <summary>
    /// Tells whether a unit fits a value kind: a number takes a physical unit, a boolean takes
    /// <see cref="BooleanName"/>, and a state set takes <see cref="StateName"/>.
    /// </summary>
    /// <param name="valueKind">The value kind of the metric.</param>
    /// <param name="unit">The unit, with its quantity kind loaded.</param>
    /// <returns><see langword="true"/> when the unit fits the kind.</returns>
    public static bool Fits(MetricValueKind valueKind, MeasurementUnit unit)
    {
        if (valueKind == MetricValueKind.Numeric)
        {
            return !IsDiscrete(unit);
        }

        var expectedName = valueKind == MetricValueKind.Boolean ? BooleanName : StateName;

        return IsDiscrete(unit) && unit.Name == expectedName;
    }
}
