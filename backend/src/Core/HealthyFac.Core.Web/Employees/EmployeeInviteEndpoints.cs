using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.SearchEmployeeInvites;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using ApplicationCreateEmployeeInviteRequest =
    HealthyFac.Core.Application.Employees.CreateEmployeeInviteRequest;
using ApplicationResendEmployeeInviteRequest =
    HealthyFac.Core.Application.Employees.ResendEmployeeInviteRequest;
using ApplicationRevokeEmployeeInviteRequest =
    HealthyFac.Core.Application.Employees.RevokeEmployeeInviteRequest;
using ApplicationSearchEmployeeInvitesRequest =
    HealthyFac.Core.Application.Employees.SearchEmployeeInvites.SearchEmployeeInvitesRequest;

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
            group.MapPost(EmployeeInviteRoutes.Search, SearchAsync)
                .RequireAuthorization(CorePermissions.Employees.Invite);
            group.MapDelete(EmployeeInviteRoutes.ById, RevokeAsync)
                .RequireAuthorization(CorePermissions.Employees.Invite);
            group.MapPost(EmployeeInviteRoutes.Verify, VerifyAsync)
                .AllowAnonymous();
            group.MapPost(EmployeeInviteRoutes.Accept, AcceptAsync)
                .AllowAnonymous();

            return app;
        }
    }

    private static async Task<IResult> SearchAsync(
        SearchEmployeeInvitesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchEmployeeInvitesRequest, GridResponse<EmployeeInviteRowResponse>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateAsync(
        CreateEmployeeInviteRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateEmployeeInviteRequest, CreateEmployeeInviteResponse> handler,
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
        IRequestHandler<ApplicationResendEmployeeInviteRequest, ResendEmployeeInviteResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<NoContent> RevokeAsync(
        [AsParameters] RevokeEmployeeInviteRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationRevokeEmployeeInviteRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
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
