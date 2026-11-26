using Grpc.Core;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Telemetry.Contracts.Definitions;

namespace HealthyFac.Insights.Infrastructure.Extraction;

/// <summary>
/// Reads the active metric keys and the status codes of a machine from Telemetry through the gRPC call
/// <c>ListDefinitions</c>. The client comes from <see cref="TelemetryDefinitionsClients"/>, so a request with a user
/// token and a background flow both work. The call runs in the downstream scope <c>telemetry</c>. A <c>NotFound</c>
/// reply gives empty lists, because the registration of the machine in Telemetry can still be missing.
/// </summary>
/// <param name="clients">Picks the Telemetry definitions client.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the call.</param>
public sealed class TelemetryMachineSignalCatalog(
    ITelemetryDefinitionsClients clients,
    IDownstreamScopeHolder downstreamScopes
) : IMachineSignalCatalog
{
    private const string UnavailableMessage = "Telemetry did not answer the definitions request.";

    private readonly ITelemetryDefinitionsClients _clients = clients;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public async Task<MachineSignals> GetAsync(Guid machineId, CancellationToken cancellationToken)
    {
        var client = _clients.Create();
        using var lease = _downstreamScopes.Enter(TelemetryClientNames.DownstreamScope);

        try
        {
            var reply = await client.ListDefinitionsAsync(
                new ListDefinitionsRequest { MachineId = machineId.ToString() },
                cancellationToken: cancellationToken);

            return new MachineSignals
            {
                Metrics = reply.Metrics.Select(metric => ToReference(metric.Id, metric.Key)).ToList(),
                Statuses = reply.Statuses.Select(status => ToReference(status.Id, status.Code)).ToList(),
            };
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return new MachineSignals { Metrics = [], Statuses = [] };
        }
        catch (RpcException exception) when (CoreWorkOrderCatalog.IsCancelledByCaller(exception, cancellationToken))
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
        catch (RpcException exception) when (GrpcTelemetrySignals.IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }

    private static SignalReference ToReference(string id, string key) => new() { Id = Guid.Parse(id), Key = key };
}
