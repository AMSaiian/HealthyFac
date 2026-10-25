using HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;
using HealthyFac.Core.Application.Employees.CreateEmployeeInvite;
using HealthyFac.Core.Application.Employees.ResendEmployeeInvite;
using HealthyFac.Core.Application.Employees.VerifyEmployeeInvite;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Web.Employees;

/// <summary>
/// Configures endpoint routing for employee invite operations.
/// </summary>
public static class EmployeeInviteEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all employee invite endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapEmployeeInviteEndpoints()
        {
            var group = app.MapGroup(EmployeeInviteRoutes.BaseRoute);
            group.MapPost(EmployeeInviteRoutes.Root, CreateAsync);
            group.MapPost(EmployeeInviteRoutes.Resend, ResendAsync);
            group.MapPost(EmployeeInviteRoutes.Verify, VerifyAsync);
            group.MapPost(EmployeeInviteRoutes.Accept, AcceptAsync);

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        CreateEmployeeInviteRequest request,
        HttpRequest httpRequest,
        IRequestHandler<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        var location = string.Format(
            EmployeeInviteRoutes.ResourceLocationFormat,
            httpRequest.Path.Value?.TrimEnd(EmployeeInviteRoutes.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<IResult> ResendAsync(
        ResendEmployeeInviteRequest request,
        IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> VerifyAsync(
        VerifyEmployeeInviteRequest request,
        IRequestHandler<VerifyEmployeeInviteRequest, VerifyEmployeeInviteResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> AcceptAsync(
        AcceptEmployeeInviteRequest request,
        IRequestHandler<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return Results.Ok(response);
    }
}
