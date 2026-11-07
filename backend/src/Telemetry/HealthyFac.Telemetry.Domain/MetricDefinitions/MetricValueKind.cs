using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// Defines the kinds of value that a metric carries. The camelCase values reach the API and the database, so never
/// rename one in place.
/// </summary>
public sealed class MetricValueKind : Enumeration<MetricValueKind>
{
    public static readonly MetricValueKind Numeric = new(value: "numeric");
    public static readonly MetricValueKind Boolean = new(value: "boolean");
    public static readonly MetricValueKind StateSet = new(value: "stateSet");

    private MetricValueKind(string value)
        : base(value)
    {
    }
}
