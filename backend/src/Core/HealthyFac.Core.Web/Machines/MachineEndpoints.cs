using HealthyFac.Core.Application.Machines.CreateMachine;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;

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
            group.MapPost(MachineRoutes.Root, CreateAsync);

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        CreateMachineRequest request,
        HttpRequest httpRequest,
        IRequestHandler<CreateMachineRequest, CreateMachineResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        var location = string.Format(
            MachineRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(MachineRoutes.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }
}
