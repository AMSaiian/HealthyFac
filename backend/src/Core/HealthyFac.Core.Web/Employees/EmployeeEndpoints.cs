using HealthyFac.Core.Application.Employees.RetryProvisioning;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using ApplicationDeactivateEmployeeRequest =
    HealthyFac.Core.Application.Employees.DeactivateEmployee.DeactivateEmployeeRequest;
using ApplicationRetryProvisioningRequest =
    HealthyFac.Core.Application.Employees.RetryProvisioning.RetryProvisioningRequest;

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

            return app;
        }
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
