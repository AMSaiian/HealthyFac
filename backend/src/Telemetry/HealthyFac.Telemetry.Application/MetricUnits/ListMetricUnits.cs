using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Domain.MetricUnits;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MetricUnits;

public record ListMetricUnitsRequest : IRequest<ListMetricUnitsResponse>;

public record ListMetricUnitsResponse
{
    public required IReadOnlyList<QuantityKindResponse> Kinds { get; init; }
}

public record QuantityKindResponse
{
    public required string Name { get; init; }

    public required IReadOnlyList<MeasurementUnitResponse> Units { get; init; }
}

public record MeasurementUnitResponse
{
    public required string Name { get; init; }

    public required string Abbreviation { get; init; }
}

/// <summary>
/// The unit catalog is global and holds no company data, so the check has no scope rule.
/// </summary>
public sealed class ListMetricUnitsPreHandlerCheck : PassThroughPreHandlerCheck<ListMetricUnitsRequest>;

/// <summary>
/// Lists the quantity kinds with their units, sorted by kind name and unit name.
/// </summary>
/// <param name="dbContext">Reads the catalog.</param>
public sealed class ListMetricUnitsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<ListMetricUnitsRequest, ListMetricUnitsResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<ListMetricUnitsResponse> HandleAsync(
        ListMetricUnitsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var units = await _dbContext.MeasurementUnits
            .AsNoTracking()
            .Where(unit => unit.QuantityKind.Name != DiscreteUnits.KindName)
            .OrderBy(unit => unit.QuantityKind.Name)
            .ThenBy(unit => unit.Name)
            .Select(unit => new { KindName = unit.QuantityKind.Name, unit.Name, unit.Abbreviation })
            .ToListAsync(cancellationToken);

        var kinds = units
            .GroupBy(unit => unit.KindName)
            .Select(group => new QuantityKindResponse
            {
                Name = group.Key,
                Units = group
                    .Select(unit => new MeasurementUnitResponse { Name = unit.Name, Abbreviation = unit.Abbreviation })
                    .ToList(),
            })
            .ToList();

        return new ListMetricUnitsResponse { Kinds = kinds };
    }
}
