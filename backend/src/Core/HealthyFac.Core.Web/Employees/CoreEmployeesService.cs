using Grpc.Core;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.Contracts.Employees;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.AspNetCore.Authorization;
using ApplicationLookupEmployeesRequest = HealthyFac.Core.Application.Employees.LookupEmployeesRequest;
using RpcLookupEmployeesRequest = HealthyFac.Core.Contracts.Employees.LookupEmployeesRequest;

namespace HealthyFac.Core.Web.Employees;

/// <summary>
/// Serves the employee lookup of the company of the actor to the services that read it (Insights) on the gRPC port.
/// The method runs the use case of its HTTP endpoint.
/// </summary>
/// <param name="lookupHandler">Looks up the employees.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
[Authorize(Policy = CorePermissions.Employees.Read)]
public sealed class CoreEmployeesService(
    IRequestHandler<ApplicationLookupEmployeesRequest, LookupResponse> lookupHandler,
    ICurrentUserProvider currentUser
) : CoreEmployees.CoreEmployeesBase
{
    private readonly IRequestHandler<ApplicationLookupEmployeesRequest, LookupResponse> _lookupHandler =
        lookupHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    public override async Task<LookupEmployeesReply> LookupEmployees(
        RpcLookupEmployeesRequest request,
        ServerCallContext context
    )
    {
        var response = await _lookupHandler.HandleAsync(
            new ApplicationLookupEmployeesRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Search = RpcInput.ReadText(request.Search),
            },
            context.CancellationToken);

        var reply = new LookupEmployeesReply();
        reply.Employees.AddRange(
            response.Items.Select(item => new EmployeeOption
                { Id = item.Id.ToString(), DisplayName = item.DisplayName }));

        return reply;
    }
}
