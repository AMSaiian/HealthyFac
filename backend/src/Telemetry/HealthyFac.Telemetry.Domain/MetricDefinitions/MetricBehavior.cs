using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// Defines how the readings of a numeric metric behave. A <see cref="Level"/> metric reports a value that moves
/// around a level (a temperature). A <see cref="Counter"/> metric only grows (a piece count) and drops back to a
/// lower value when the machine restarts. The camelCase values reach the API and the database, so never rename one
/// in place.
/// </summary>
public sealed class MetricBehavior : Enumeration<MetricBehavior>
{
    public static readonly MetricBehavior Level = new(value: "level");
    public static readonly MetricBehavior Counter = new(value: "counter");

    private MetricBehavior(string value)
        : base(value)
    {
    }
}
