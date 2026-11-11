using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.SearchEmployees;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using ApplicationDeactivateEmployeeRequest =
    HealthyFac.Core.Application.Employees.DeactivateEmployeeRequest;
using ApplicationRetryProvisioningRequest =
    HealthyFac.Core.Application.Employees.RetryProvisioningRequest;
using ApplicationGetCurrentEmployeeRequest = HealthyFac.Core.Application.Employees.GetCurrentEmployeeRequest;
using ApplicationLookupEmployeesRequest = HealthyFac.Core.Application.Employees.LookupEmployeesRequest;
using ApplicationSearchEmployeesRequest = HealthyFac.Core.Application.Employees.SearchEmployees.SearchEmployeesRequest;

namespace HealthyFac.Core.Web.Employees;

/// <summary>
/// Configures endpoint routing for employee operations.
/// </summary>
public static class EmployeeEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all employee endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapEmployeeEndpoints()
        {
            var group = app.MapGroup(EmployeeRoutes.BaseRoute);
            group.MapPost(EmployeeRoutes.RetryProvisioning, RetryProvisioningAsync)
                .RequireAuthorization(CorePermissions.Employees.Manage);
            group.MapPost(EmployeeRoutes.Deactivate, DeactivateAsync)
                .RequireAuthorization(CorePermissions.Employees.Manage);
            group.MapGet(EmployeeRoutes.Me, GetCurrentAsync);
            group.MapPost(EmployeeRoutes.Search, SearchAsync)
                .RequireAuthorization(CorePermissions.Employees.Read);
            group.MapPost(EmployeeRoutes.Lookup, LookupAsync)
                .RequireAuthorization(CorePermissions.Employees.Read);

            return app;
        }
    }

    private static async Task<IResult> GetCurrentAsync(
        IRequestHandler<ApplicationGetCurrentEmployeeRequest, CurrentEmployeeResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(new ApplicationGetCurrentEmployeeRequest(), cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> LookupAsync(
        LookupEmployeesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationLookupEmployeesRequest, LookupResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> SearchAsync(
        SearchEmployeesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchEmployeesRequest, GridResponse<EmployeeRowResponse>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> RetryProvisioningAsync(
        RetryProvisioningRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationRetryProvisioningRequest, RetryProvisioningResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> DeactivateAsync(
        DeactivateEmployeeRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeactivateEmployeeRequest> handler,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(request.ToApplicationRequest(currentUser.GetCompanyId()), cancellationToken);

        return Results.NoContent();
    }
}
