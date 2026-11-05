using HealthyFac.Core.Application.Departments;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HealthyFac.Core.Web.Departments;

/// <summary>
/// Configures endpoint routing for department operations.
/// </summary>
public static class DepartmentEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all department endpoints to the application route builder. Any signed-in user may read the list,
        /// because it holds only the departments of that user.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapDepartmentEndpoints()
        {
            var group = app.MapGroup(DepartmentRoutes.BaseRoute);
            group.MapGet(DepartmentRoutes.Root, ListAsync);

            return app;
        }
    }

    private static async Task<Ok<ListEmployeeDepartmentsResponse>> ListAsync(
        IRequestHandler<ListEmployeeDepartmentsRequest, ListEmployeeDepartmentsResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(new ListEmployeeDepartmentsRequest(), cancellationToken);

        return TypedResults.Ok(response);
    }
}
