using System.Net.Http.Json;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Fetches the unit catalog from Telemetry (<c>GET /v1/metric-units</c>) with the service token. The call enters the
/// downstream scope <c>telemetry</c> for the call only.
/// </summary>
/// <param name="httpClientFactory">Creates the Telemetry client.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the call.</param>
public sealed class TelemetryMetricUnitClient(
    IHttpClientFactory httpClientFactory,
    IDownstreamScopeHolder downstreamScopes
) : IMetricUnitSource
{
    private const string UnitsPath = "v1/metric-units";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public async Task<IReadOnlyList<MetricUnit>> FetchAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(TelemetryClientNames.Client);
        using var lease = _downstreamScopes.Enter(TelemetryClientNames.DownstreamScope);

        using var response = await client.GetAsync(UnitsPath, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);

        var catalog = await response.Content.ReadFromJsonAsync<CatalogPayload>(cancellationToken);

        return catalog is null
            ? []
            : catalog.Kinds
                .SelectMany(kind => kind.Units.Select(unit => new MetricUnit
                {
                    QuantityKind = kind.Name,
                    Name = unit.Name,
                    Abbreviation = unit.Abbreviation,
                }))
                .ToList();
    }

    private sealed record CatalogPayload
    {
        public required IReadOnlyList<KindPayload> Kinds { get; init; }
    }

    private sealed record KindPayload
    {
        public required string Name { get; init; }

        public required IReadOnlyList<UnitPayload> Units { get; init; }
    }

    private sealed record UnitPayload
    {
        public required string Name { get; init; }

        public required string Abbreviation { get; init; }
    }
}
