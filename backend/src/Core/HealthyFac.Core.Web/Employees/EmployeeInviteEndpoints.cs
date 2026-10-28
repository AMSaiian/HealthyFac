using HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;
using HealthyFac.Core.Application.Employees.CreateEmployeeInvite;
using HealthyFac.Core.Application.Employees.ResendEmployeeInvite;
using HealthyFac.Core.Application.Employees.VerifyEmployeeInvite;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
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
            group.MapPost(EmployeeInviteRoutes.Root, CreateAsync)
                .RequireAuthorization(CorePermissions.Employees.Invite);
            group.MapPost(EmployeeInviteRoutes.Resend, ResendAsync)
                .RequireAuthorization(CorePermissions.Employees.Invite);
            group.MapPost(EmployeeInviteRoutes.Verify, VerifyAsync)
                .AllowAnonymous();
            group.MapPost(EmployeeInviteRoutes.Accept, AcceptAsync)
                .AllowAnonymous();

            return app;
        }
    }

    private static async Task<IResult> CreateAsync(
        CreateEmployeeInviteRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<
            Application.Employees.CreateEmployeeInvite.CreateEmployeeInviteRequest,
            CreateEmployeeInviteResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        var location = string.Format(
            EmployeeInviteRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<IResult> ResendAsync(
        ResendEmployeeInviteRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<
            Application.Employees.ResendEmployeeInvite.ResendEmployeeInviteRequest,
            ResendEmployeeInviteResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

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
