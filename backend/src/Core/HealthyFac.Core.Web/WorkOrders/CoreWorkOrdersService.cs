using Grpc.Core;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.AspNetCore.Authorization;
using ApplicationGetWorkOrderHistoryRequest = HealthyFac.Core.Application.WorkOrders.GetWorkOrderHistoryRequest;
using ApplicationGetWorkOrderRequest = HealthyFac.Core.Application.WorkOrders.GetWorkOrderRequest;
using ApplicationSearchWorkOrdersRequest =
    HealthyFac.Core.Application.WorkOrders.SearchWorkOrders.SearchWorkOrdersRequest;
using RpcGetWorkOrderRequest = HealthyFac.Core.Contracts.WorkOrders.GetWorkOrderRequest;
using RpcSearchWorkOrdersRequest = HealthyFac.Core.Contracts.WorkOrders.SearchWorkOrdersRequest;

namespace HealthyFac.Core.Web.WorkOrders;

/// <summary>
/// Serves the work orders to the services that read them (Insights) on the gRPC port. Each method runs the use case of
/// its HTTP endpoint with the access scope of the token: a token without `work-orders:read-company` reaches only the
/// work orders of its employee, and a work order outside the scope is "not found".
/// </summary>
/// <param name="searchHandler">Searches the work orders.</param>
/// <param name="getHandler">Reads one work order.</param>
/// <param name="historyHandler">Reads the history of a work order.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
[Authorize(Policy = CorePermissions.WorkOrders.Read)]
public sealed class CoreWorkOrdersService(
    IRequestHandler<ApplicationSearchWorkOrdersRequest, GridResponse<WorkOrderRowResponse>> searchHandler,
    IRequestHandler<ApplicationGetWorkOrderRequest, WorkOrderResponse> getHandler,
    IRequestHandler<ApplicationGetWorkOrderHistoryRequest, WorkOrderHistoryPageResponse> historyHandler,
    ICurrentUserProvider currentUser
) : CoreWorkOrders.CoreWorkOrdersBase
{
    private readonly IRequestHandler<ApplicationSearchWorkOrdersRequest, GridResponse<WorkOrderRowResponse>>
        _searchHandler = searchHandler;

    private readonly IRequestHandler<ApplicationGetWorkOrderRequest, WorkOrderResponse> _getHandler = getHandler;

    private readonly IRequestHandler<ApplicationGetWorkOrderHistoryRequest, WorkOrderHistoryPageResponse>
        _historyHandler = historyHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    public override async Task<SearchWorkOrdersReply> SearchWorkOrders(
        RpcSearchWorkOrdersRequest request,
        ServerCallContext context
    )
    {
        var statuses = RpcInput.ParseEnumeration<WorkOrderStatus>(request.Statuses, nameof(request.Statuses));
        var assigneeId = RpcInput.ParseOptionalId(request.AssigneeId);
        var sort = ParseSort(request);
        var machineId = RpcInput.ParseOptionalId(request.MachineId);

        var response = await _searchHandler.HandleAsync(
            new ApplicationSearchWorkOrdersRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Scope = AccessScopes.Resolve(context.GetHttpContext().User, CorePermissions.WorkOrders.ReadCompany),
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = sort,
                Filter = new WorkOrderGridFilter
                {
                    Statuses = statuses.Count == 0 ? null : new SetFilter<WorkOrderStatus> { Values = statuses },
                    Assignee = assigneeId is { } assignee ? new IdSetFilter { Ids = [assignee] } : null,
                    Machine = machineId is { } machine ? new IdSetFilter { Ids = [machine] } : null,
                    PlannedStart = request.PlannedFrom is null && request.PlannedTo is null
                        ? null
                        : new DateRangeFilter
                        {
                            From = request.PlannedFrom?.ToDateTime(),
                            To = request.PlannedTo?.ToDateTime(),
                        },
                },
            },
            context.CancellationToken);

        var reply = new SearchWorkOrdersReply { TotalCount = response.TotalCount };
        reply.WorkOrders.AddRange(response.Items.Select(ToRow));

        return reply;
    }

    public override async Task<WorkOrderReply> GetWorkOrder(RpcGetWorkOrderRequest request, ServerCallContext context)
    {
        var workOrderId = RpcInput.ParseId(request.WorkOrderId);
        var companyId = _currentUser.GetCompanyId();
        var scope = AccessScopes.Resolve(context.GetHttpContext().User, CorePermissions.WorkOrders.ReadCompany);

        var workOrder = await _getHandler.HandleAsync(
            new ApplicationGetWorkOrderRequest { CompanyId = companyId, Scope = scope, Id = workOrderId },
            context.CancellationToken);

        var history = await _historyHandler.HandleAsync(
            new ApplicationGetWorkOrderHistoryRequest
            {
                CompanyId = companyId,
                Scope = scope,
                Id = workOrderId,
                Limit = request.HistoryLimit,
            },
            context.CancellationToken);

        var reply = new WorkOrderReply
        {
            Id = workOrder.Id.ToString(),
            Title = workOrder.Title,
            Status = workOrder.Status.ToString(),
            Priority = workOrder.Priority.ToString(),
            Category = workOrder.Category.ToString(),
            EstimateMinutes = workOrder.EstimateMinutes,
            IsDelayed = workOrder.IsDelayed,
            CreatedAt = RpcTimestamps.ToTimestamp(workOrder.CreatedAt),
            PlannedStart = RpcTimestamps.ToTimestamp(workOrder.PlannedStartUtc),
            PlannedEnd = RpcTimestamps.ToTimestamp(PlannedEnd(workOrder.PlannedStartUtc, workOrder.EstimateMinutes)),
        };

        if (workOrder.Description is { } descriptionValue)
        {
            reply.Description = descriptionValue;
        }

        if (workOrder.MachineId?.ToString() is { } machineIdValue)
        {
            reply.MachineId = machineIdValue;
        }

        if (workOrder.MachineName is { } machineNameValue)
        {
            reply.MachineName = machineNameValue;
        }

        if (workOrder.AssigneeId?.ToString() is { } assigneeIdValue)
        {
            reply.AssigneeId = assigneeIdValue;
        }

        if (workOrder.AssigneeName is { } assigneeNameValue)
        {
            reply.AssigneeName = assigneeNameValue;
        }

        reply.History.AddRange(history.Items.Select(ToHistoryRow));

        return reply;
    }

    private static GridSort<WorkOrderSortColumn>? ParseSort(RpcSearchWorkOrdersRequest request)
    {
        if (request.SortColumn.Length == 0)
        {
            return null;
        }

        var column = RpcInput.ParseEnumeration<WorkOrderSortColumn>([request.SortColumn], nameof(request.SortColumn));

        var direction = request.SortDirection.Length == 0
            ? [SortDirection.Ascending]
            : RpcInput.ParseEnumeration<SortDirection>([request.SortDirection], nameof(request.SortDirection));

        return new GridSort<WorkOrderSortColumn> { Column = column[0], Direction = direction[0] };
    }

    private static WorkOrderRow ToRow(WorkOrderRowResponse row)
    {
        var reply = new WorkOrderRow
        {
            Id = row.Id.ToString(),
            Title = row.Title,
            Status = row.Status.ToString(),
            Priority = row.Priority.ToString(),
            Category = row.Category.ToString(),
            PlannedStart = RpcTimestamps.ToTimestamp(row.PlannedStartUtc),
            PlannedEnd = RpcTimestamps.ToTimestamp(PlannedEnd(row.PlannedStartUtc, row.EstimateMinutes)),
            EstimateMinutes = row.EstimateMinutes,
        };

        if (row.MachineId?.ToString() is { } machineIdValue)
        {
            reply.MachineId = machineIdValue;
        }

        if (row.AssigneeId?.ToString() is { } assigneeIdValue)
        {
            reply.AssigneeId = assigneeIdValue;
        }

        if (row.AssigneeName is { } assigneeNameValue)
        {
            reply.AssigneeName = assigneeNameValue;
        }

        return reply;
    }

    private static WorkOrderHistoryRow ToHistoryRow(WorkOrderHistoryResponse row)
    {
        var reply = new WorkOrderHistoryRow
        {
            Revision = row.Revision,
            Status = row.Status.ToString(),
            PlannedStart = RpcTimestamps.ToTimestamp(row.PlannedStartUtc),
            EstimateMinutes = row.EstimateMinutes,
            CreatedAt = RpcTimestamps.ToTimestamp(row.CreatedAt),
        };

        if (row.AssigneeId?.ToString() is { } assigneeIdValue)
        {
            reply.AssigneeId = assigneeIdValue;
        }

        if (row.AssigneeName is { } assigneeNameValue)
        {
            reply.AssigneeName = assigneeNameValue;
        }

        return reply;
    }

    private static DateTime? PlannedEnd(DateTime? plannedStartUtc, int estimateMinutes) =>
        plannedStartUtc?.AddMinutes(estimateMinutes);
}
