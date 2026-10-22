using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using ApplicationRequest = HealthyFac.Core.Application.Machines.CreateMachine.CreateMachineRequest;

namespace HealthyFac.Core.Web.Machines.CreateMachine;

/// <summary>
/// Defines endpoint mappings for creating a machine.
/// </summary>
public static class CreateMachineEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the create machine endpoint to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapCreateMachineEndpoint()
        {
            app.MapPost(
                MachineRoutes.Root, async (
                    CreateMachineRequest request,
                    HttpRequest httpRequest,
                    IRequestHandler<ApplicationRequest, Guid> handler,
                    IIdEncryptor idEncryptor,
                    CancellationToken cancellationToken
                ) =>
                {
                    var id = await handler.HandleAsync(
                        new ApplicationRequest
                        {
                            Name = request.Name,
                            SerialNumber = request.SerialNumber,
                            CompanyId = request.CompanyId,
                            DepartmentId = request.DepartmentId,
                        },
                        cancellationToken);

                    var location = string.Format(
                        MachineRoutes.ResourceLocationFormat,
                        httpRequest.Path.Value?.TrimEnd(MachineRoutes.PathSeparator),
                        idEncryptor.Encrypt(id));

                    return Results.Created(location, new CreateMachineResponse { Id = id });
                });

            return app;
        }
    }
}
