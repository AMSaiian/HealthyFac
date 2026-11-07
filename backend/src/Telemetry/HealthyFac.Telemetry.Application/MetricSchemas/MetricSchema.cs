using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Application.MetricSchemas;

/// <summary>
/// The part of a metric definition that the payload schema needs.
/// </summary>
public sealed record MetricSchema
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required MetricValueKind ValueKind { get; init; }

    /// <summary>The quantity kind name of the unit of a numeric metric.</summary>
    public string? UnitQuantity { get; init; }

    /// <summary>The unit name of a numeric metric.</summary>
    public string? UnitName { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }
}
