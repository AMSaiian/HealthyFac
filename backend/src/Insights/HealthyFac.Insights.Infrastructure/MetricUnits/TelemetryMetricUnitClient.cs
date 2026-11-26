using Grpc.Core;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Telemetry.Contracts.Definitions;

namespace HealthyFac.Insights.Infrastructure.MetricUnits;

/// <summary>
/// Fetches the unit catalog from Telemetry through the gRPC call <c>ListMetricUnits</c>. The client comes from
/// <see cref="TelemetryDefinitionsClients"/>, so a request with a user token and a background flow both work. The call
/// enters the downstream scope <c>telemetry</c> for the call only.
/// </summary>
/// <param name="clients">Picks the Telemetry definitions client.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the call.</param>
public sealed class TelemetryMetricUnitClient(
    ITelemetryDefinitionsClients clients,
    IDownstreamScopeHolder downstreamScopes
) : IMetricUnitSource
{
    private const string UnavailableMessage = "Telemetry did not answer the unit catalog request.";

    private readonly ITelemetryDefinitionsClients _clients = clients;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public async Task<IReadOnlyList<MetricUnit>> FetchAsync(CancellationToken cancellationToken)
    {
        var client = _clients.Create();
        using var lease = _downstreamScopes.Enter(TelemetryClientNames.DownstreamScope);

        try
        {
            var reply = await client.ListMetricUnitsAsync(
                new ListMetricUnitsRequest(),
                cancellationToken: cancellationToken);

            return
            [
                .. reply.Kinds
                    .SelectMany(kind => kind.Units.Select(unit => new MetricUnit
                    {
                        QuantityKind = kind.Name,
                        Name = unit.Name,
                        Abbreviation = unit.Abbreviation,
                    })),
            ];
        }
        catch (RpcException exception) when (RpcFailures.IsCancelledByCaller(exception, cancellationToken))
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
        catch (RpcException exception) when (RpcFailures.IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }
}
