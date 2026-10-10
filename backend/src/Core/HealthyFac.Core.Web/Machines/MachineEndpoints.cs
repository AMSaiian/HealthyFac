using HealthyFac.Core.Web.Machines.CreateMachine;

namespace HealthyFac.Core.Web.Machines;

/// <summary>
/// Configures endpoint routing for machine business operations.
/// </summary>
public static class MachineEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all machine endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapMachineEndpoints()
        {
            var group = app.MapGroup(MachineRoutes.BaseRoute);
            group.MapCreateMachineEndpoint();

            return app;
        }
    }
}
