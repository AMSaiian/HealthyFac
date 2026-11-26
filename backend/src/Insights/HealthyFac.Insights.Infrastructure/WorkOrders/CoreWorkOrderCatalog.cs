using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Infrastructure.Rpc;

namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Reads the open work orders of a machine from Core through its gRPC port (<c>SearchWorkOrders</c>). The call uses the
/// service token with the actor headers, and enters the downstream scope of the call for the call only. The reader
/// holds no assignee. A call that the caller cancelled ends in an <see cref="OperationCanceledException"/>, not in a
/// failure for the model. An outage (the status <c>Unavailable</c> or <c>DeadlineExceeded</c>) ends in a
/// <see cref="ProviderUnavailableException"/>, so a planner run fails as unavailable instead of going on without the
/// data. Any other failure stays an <see cref="RpcException"/> for the model.
/// </summary>
/// <param name="clientFactory">Creates the named client.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the call.</param>
public sealed class CoreWorkOrderCatalog(GrpcClientFactory clientFactory, IDownstreamScopeHolder downstreamScopes)
    : IWorkOrderCatalog
{
    internal const string CreatedAtColumn = "createdAt";
    internal const string DescendingDirection = "desc";

    internal static readonly string[] OpenStatuses = ["backlog", "staged", "inProgress", "blocked"];

    private const string UnavailableMessage = "Core did not answer the open work orders request.";

    private readonly GrpcClientFactory _clientFactory = clientFactory;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public async Task<IReadOnlyList<OpenWorkOrderInfo>> ListOpenAsync(
        Guid machineId,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var client = _clientFactory.CreateClient<CoreWorkOrders.CoreWorkOrdersClient>(CoreClientNames.Worker);

        using var lease = _downstreamScopes.Enter(CoreClientNames.DownstreamScope);

        try
        {
            var reply = await client.SearchWorkOrdersAsync(
                new SearchWorkOrdersRequest
                {
                    Statuses = { OpenStatuses },
                    MachineId = machineId.ToString(),
                    Limit = limit,
                    SortColumn = CreatedAtColumn,
                    SortDirection = DescendingDirection,
                },
                cancellationToken: cancellationToken);

            return [.. reply.WorkOrders.Select(ToInfo)];
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

    private static OpenWorkOrderInfo ToInfo(WorkOrderRow row) =>
        new()
        {
            Id = Guid.Parse(row.Id),
            Title = row.Title,
            Status = row.Status,
            Priority = row.Priority,
            Category = row.Category,
            PlannedStartUtc = row.PlannedStart?.ToDateTime(),
            PlannedEndUtc = row.PlannedEnd?.ToDateTime(),
        };
}
