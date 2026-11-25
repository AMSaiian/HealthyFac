using Grpc.Core;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Application.Machines.States;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.AspNetCore.Authorization;
using ApplicationGetMachineRequest = HealthyFac.Core.Application.Machines.GetMachineRequest;
using ApplicationGetMachineStateTimesRequest = HealthyFac.Core.Application.Machines.States.GetMachineStateTimesRequest;
using ApplicationSearchMachinesRequest = HealthyFac.Core.Application.Machines.SearchMachines.SearchMachinesRequest;
using RpcGetMachineRequest = HealthyFac.Core.Contracts.Machines.GetMachineRequest;
using RpcGetMachineStateTimesRequest = HealthyFac.Core.Contracts.Machines.GetMachineStateTimesRequest;
using RpcSearchMachinesRequest = HealthyFac.Core.Contracts.Machines.SearchMachinesRequest;

namespace HealthyFac.Core.Web.Machines;

/// <summary>
/// Serves the machines of the company of the actor to the services that read them (Insights) on the gRPC port. Each
/// method runs the use case of its HTTP endpoint. A user token or a service token with actor headers works.
/// </summary>
/// <param name="searchHandler">Searches the machines.</param>
/// <param name="getHandler">Reads one machine.</param>
/// <param name="stateTimesHandler">Reads the time of a machine in each state.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
[Authorize(Policy = CorePermissions.Machines.Read)]
public sealed class CoreMachinesService(
    IRequestHandler<ApplicationSearchMachinesRequest, GridResponse<MachineResponse>> searchHandler,
    IRequestHandler<ApplicationGetMachineRequest, MachineResponse> getHandler,
    IRequestHandler<ApplicationGetMachineStateTimesRequest, MachineStateTimesResponse> stateTimesHandler,
    ICurrentUserProvider currentUser
) : CoreMachines.CoreMachinesBase
{
    private readonly IRequestHandler<ApplicationSearchMachinesRequest, GridResponse<MachineResponse>> _searchHandler =
        searchHandler;

    private readonly IRequestHandler<ApplicationGetMachineRequest, MachineResponse> _getHandler = getHandler;

    private readonly IRequestHandler<ApplicationGetMachineStateTimesRequest, MachineStateTimesResponse>
        _stateTimesHandler = stateTimesHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    public override async Task<SearchMachinesReply> SearchMachines(
        RpcSearchMachinesRequest request,
        ServerCallContext context
    )
    {
        var states = RpcInput.ParseEnumeration<MachineState>(request.States, nameof(request.States));
        var name = RpcInput.ReadText(request.Name);
        var serialNumber = RpcInput.ReadText(request.SerialNumber);

        var response = await _searchHandler.HandleAsync(
            new ApplicationSearchMachinesRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Offset = request.Offset,
                Limit = request.Limit,
                Filter = new MachineGridFilter
                {
                    Name = name is null ? null : new TextFilter { Value = name },
                    SerialNumber = serialNumber is null ? null : new TextFilter { Value = serialNumber },
                    States = states.Count == 0 ? null : new SetFilter<MachineState> { Values = states },
                },
            },
            context.CancellationToken);

        var reply = new SearchMachinesReply { TotalCount = response.TotalCount };
        reply.Machines.AddRange(response.Items.Select(ToReply));

        return reply;
    }

    public override async Task<MachineReply> GetMachine(RpcGetMachineRequest request, ServerCallContext context)
    {
        var response = await _getHandler.HandleAsync(
            new ApplicationGetMachineRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Id = RpcInput.ParseId(request.MachineId),
            },
            context.CancellationToken);

        return ToReply(response);
    }

    public override async Task<MachineStateTimesReply> GetMachineStateTimes(
        RpcGetMachineStateTimesRequest request,
        ServerCallContext context
    )
    {
        var response = await _stateTimesHandler.HandleAsync(
            new ApplicationGetMachineStateTimesRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                MachineId = RpcInput.ParseId(request.MachineId),
                From = RpcInput.ParseRequiredTimestamp(request.From, nameof(request.From)),
                To = RpcInput.ParseRequiredTimestamp(request.To, nameof(request.To)),
            },
            context.CancellationToken);

        var reply = new MachineStateTimesReply { BeforeCreationSeconds = response.BeforeCreationSeconds };

        reply.States.AddRange(
            response.States.Select(state => new MachineStateTime { State = state.State, Seconds = state.Seconds }));

        return reply;
    }

    private static MachineReply ToReply(MachineResponse machine)
    {
        var reply = new MachineReply
        {
            Id = machine.Id.ToString(),
            Name = machine.Name,
            SerialNumber = machine.SerialNumber,
            GatewayId = machine.GatewayId,
            State = machine.State.ToString(),
            ExpectedGoLiveOn = RpcInput.ToDateText(machine.ExpectedGoLiveOn),
        };

        if (machine.Description is { } description)
        {
            reply.Description = description;
        }

        return reply;
    }
}
