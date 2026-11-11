namespace HealthyFac.Insights.Application.MetricUnits;

/// <summary>
/// One measurement unit of the catalog that Telemetry owns.
/// </summary>
public record MetricUnit
{
    /// <summary>The quantity kind of units that have no measurement scale. The extraction never uses them.</summary>
    public const string DiscreteKind = "Discrete";

    public required string QuantityKind { get; init; }

    public required string Name { get; init; }

    public required string Abbreviation { get; init; }

    /// <summary>Tells whether the unit has no measurement scale (the kind <see cref="DiscreteKind"/>).</summary>
    public bool IsDiscrete => string.Equals(QuantityKind, DiscreteKind, StringComparison.OrdinalIgnoreCase);
}
