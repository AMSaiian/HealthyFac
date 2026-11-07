namespace HealthyFac.Telemetry.Infrastructure.MetricUnits.Persistence;

/// <summary>
/// One unit of the catalog seed.
/// </summary>
internal sealed record MetricUnitSeedRow
{
    public required string QuantityName { get; init; }

    public required string UnitName { get; init; }

    public required string Abbreviation { get; init; }
}
