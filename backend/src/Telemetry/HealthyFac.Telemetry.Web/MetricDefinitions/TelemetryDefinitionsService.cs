using Grpc.Core;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Web.Rpc;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Contracts.Definitions;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using ApplicationListMetricDefinitionsRequest =
    HealthyFac.Telemetry.Application.MetricDefinitions.ListMetricDefinitionsRequest;
using ApplicationListMetricUnitsRequest = HealthyFac.Telemetry.Application.MetricUnits.ListMetricUnitsRequest;
using ApplicationListMetricUnitsResponse = HealthyFac.Telemetry.Application.MetricUnits.ListMetricUnitsResponse;
using ApplicationSearchStatusDefinitionsRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions.SearchStatusDefinitionsRequest;
using RpcMetricDefinition = HealthyFac.Telemetry.Contracts.Definitions.MetricDefinition;
using RpcStatusDefinition = HealthyFac.Telemetry.Contracts.Definitions.StatusDefinition;

namespace HealthyFac.Telemetry.Web.MetricDefinitions;

/// <summary>
/// Serves the active metric definitions and the status definitions of a machine to the services that read them
/// (Insights) on the gRPC port. The caller sends the machine ID as a plain GUID in text. The actor company scopes the
/// read, and a machine of another company is "not found".
/// </summary>
/// <remarks>
/// The status definitions come from the status definition grid, page by page at its maximum limit, until a page holds
/// fewer items than the limit.
/// </remarks>
/// <param name="metricsHandler">Lists the active metric definitions.</param>
/// <param name="statusesHandler">Searches the status definitions.</param>
/// <param name="unitsHandler">Lists the global unit catalog.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class TelemetryDefinitionsService(
    IRequestHandler<ApplicationListMetricDefinitionsRequest, ListMetricDefinitionsResponse> metricsHandler,
    IRequestHandler<ApplicationSearchStatusDefinitionsRequest, GridResponse<StatusDefinitionResponse>> statusesHandler,
    IRequestHandler<ApplicationListMetricUnitsRequest, ApplicationListMetricUnitsResponse> unitsHandler,
    ICurrentUserProvider currentUser
) : TelemetryDefinitions.TelemetryDefinitionsBase
{
    private readonly IRequestHandler<ApplicationListMetricDefinitionsRequest, ListMetricDefinitionsResponse>
        _metricsHandler = metricsHandler;

    private readonly IRequestHandler<ApplicationSearchStatusDefinitionsRequest, GridResponse<StatusDefinitionResponse>>
        _statusesHandler = statusesHandler;

    private readonly IRequestHandler<ApplicationListMetricUnitsRequest, ApplicationListMetricUnitsResponse>
        _unitsHandler = unitsHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    [Authorize(Policy = TelemetryPermissions.MetricDefinitions.Read)]
    [Authorize(Policy = TelemetryPermissions.StatusDefinitions.Read)]
    public override async Task<ListDefinitionsReply> ListDefinitions(
        ListDefinitionsRequest request,
        ServerCallContext context
    )
    {
        var companyId = _currentUser.GetCompanyId();
        var machineId = RpcInput.ParseId(request.MachineId);

        var metrics = await _metricsHandler.HandleAsync(
            new ApplicationListMetricDefinitionsRequest { CompanyId = companyId, MachineId = machineId },
            context.CancellationToken);

        var statuses = await ReadStatusesAsync(companyId, machineId, context.CancellationToken);

        var reply = new ListDefinitionsReply();
        reply.Metrics.AddRange(metrics.Definitions.Select(ToMetric));

        reply.Statuses.AddRange(
            statuses.Select(status => new RpcStatusDefinition
            {
                Id = status.Id.ToString(),
                Code = status.Code,
                Severity = status.Severity.Value,
            }));

        return reply;
    }

    [Authorize(Policy = TelemetryPermissions.MetricDefinitions.Read)]
    public override async Task<ListMetricUnitsReply> ListMetricUnits(
        ListMetricUnitsRequest request,
        ServerCallContext context
    )
    {
        var catalog = await _unitsHandler.HandleAsync(
            new ApplicationListMetricUnitsRequest(),
            context.CancellationToken);

        var reply = new ListMetricUnitsReply();

        reply.Kinds.AddRange(
            catalog.Kinds.Select(kind =>
            {
                var message = new QuantityKind { Name = kind.Name };

                message.Units.AddRange(
                    kind.Units.Select(unit => new MeasurementUnit
                        { Name = unit.Name, Abbreviation = unit.Abbreviation }));

                return message;
            }));

        return reply;
    }

    private async Task<List<StatusDefinitionResponse>> ReadStatusesAsync(
        Guid companyId,
        Guid machineId,
        CancellationToken cancellationToken
    )
    {
        var statuses = new List<StatusDefinitionResponse>();

        while (true)
        {
            var page = await _statusesHandler.HandleAsync(
                new ApplicationSearchStatusDefinitionsRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    Offset = statuses.Count,
                    Limit = GridValidationConstants.MaxLimit,
                },
                cancellationToken);

            statuses.AddRange(page.Items);

            if (page.Items.Count < GridValidationConstants.MaxLimit)
            {
                return statuses;
            }
        }
    }

    private static RpcMetricDefinition ToMetric(MetricDefinitionResponse metric)
    {
        var message = new RpcMetricDefinition
        {
            Id = metric.Id.ToString(),
            Key = metric.Key,
            Name = metric.Name,
            ValueKind = metric.ValueKind.Value,
            Unit = metric.Unit.Abbreviation,
        };

        if (metric.Min is { } min)
        {
            message.Min = min;
        }

        if (metric.Max is { } max)
        {
            message.Max = max;
        }

        if (metric.States is { } states)
        {
            message.States.AddRange(states);
        }

        return message;
    }
}
