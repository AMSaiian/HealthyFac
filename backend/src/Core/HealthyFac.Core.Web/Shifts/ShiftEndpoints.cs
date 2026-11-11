using System.Security.Claims;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationCreateShiftTemplateVersionRequest =
    HealthyFac.Core.Application.Shifts.CreateShiftTemplateVersionRequest;
using ApplicationDeleteShiftOverrideRequest = HealthyFac.Core.Application.Shifts.DeleteShiftOverrideRequest;
using ApplicationDeleteShiftTemplateVersionRequest =
    HealthyFac.Core.Application.Shifts.DeleteShiftTemplateVersionRequest;
using ApplicationGetEmployeeDayScheduleRequest = HealthyFac.Core.Application.Shifts.GetEmployeeDayScheduleRequest;
using ApplicationListShiftOverridesRequest = HealthyFac.Core.Application.Shifts.ListShiftOverridesRequest;
using ApplicationListShiftTemplateVersionsRequest =
    HealthyFac.Core.Application.Shifts.ListShiftTemplateVersionsRequest;
using ApplicationSetShiftOverrideRequest = HealthyFac.Core.Application.Shifts.SetShiftOverrideRequest;
using ApplicationUpdateShiftTemplateVersionRequest =
    HealthyFac.Core.Application.Shifts.UpdateShiftTemplateVersionRequest;

namespace HealthyFac.Core.Web.Shifts;

/// <summary>
/// Configures endpoint routing for shift templates, overrides, and day schedules.
/// </summary>
public static class ShiftEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all shift endpoints to the application route builder. A read needs <c>shifts:read</c> and answers by
        /// the access scope of the token; a write needs <c>shifts:manage</c>.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapShiftEndpoints()
        {
            var group = app.MapGroup(ShiftRoutes.EmployeeRoute);
            group.MapGet(ShiftRoutes.Templates, ListTemplatesAsync)
                .RequireAuthorization(CorePermissions.Shifts.Read);
            group.MapPost(ShiftRoutes.Templates, CreateTemplateAsync)
                .RequireAuthorization(CorePermissions.Shifts.Manage);
            group.MapPut(ShiftRoutes.TemplateById, UpdateTemplateAsync)
                .RequireAuthorization(CorePermissions.Shifts.Manage);
            group.MapDelete(ShiftRoutes.TemplateById, DeleteTemplateAsync)
                .RequireAuthorization(CorePermissions.Shifts.Manage);
            group.MapGet(ShiftRoutes.Overrides, ListOverridesAsync)
                .RequireAuthorization(CorePermissions.Shifts.Read);
            group.MapPut(ShiftRoutes.OverrideByDate, SetOverrideAsync)
                .RequireAuthorization(CorePermissions.Shifts.Manage);
            group.MapDelete(ShiftRoutes.OverrideByDate, DeleteOverrideAsync)
                .RequireAuthorization(CorePermissions.Shifts.Manage);
            group.MapGet(ShiftRoutes.Schedule, GetEmployeeDayAsync)
                .RequireAuthorization(CorePermissions.Shifts.Read);

            return app;
        }
    }

    private static async Task<Ok<IReadOnlyList<ShiftTemplateVersionResponse>>> ListTemplatesAsync(
        [AsParameters] ListShiftTemplateVersionsRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationListShiftTemplateVersionsRequest, IReadOnlyList<ShiftTemplateVersionResponse>>
            handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.Shifts.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> CreateTemplateAsync(
        [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)] string id,
        CreateShiftTemplateVersionRequest request,
        HttpRequest httpRequest,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateShiftTemplateVersionRequest, CreateShiftTemplateVersionResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(id, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        var location = string.Format(
            ShiftRoutes.ResourceLocationFormat,
            (httpRequest.PathBase + httpRequest.Path).Value?.TrimEnd(LinkConstants.PathSeparator),
            idEncryptor.Encrypt(response.Id));

        return Results.Created(location, response);
    }

    private static async Task<NoContent> UpdateTemplateAsync(
        [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)] string id,
        [FromRoute(Name = ShiftRoutes.VersionIdParameter)]
        string versionId,
        UpdateShiftTemplateVersionRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationUpdateShiftTemplateVersionRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, versionId, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteTemplateAsync(
        [AsParameters] DeleteShiftTemplateVersionRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteShiftTemplateVersionRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<ShiftOverrideResponse>>> ListOverridesAsync(
        [AsParameters] ListShiftOverridesRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationListShiftOverridesRequest, IReadOnlyList<ShiftOverrideResponse>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.Shifts.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> SetOverrideAsync(
        [FromRoute(Name = ShiftRoutes.EmployeeIdParameter)] string id,
        [FromRoute(Name = ShiftRoutes.DateParameter)]
        DateOnly date,
        SetShiftOverrideRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSetShiftOverrideRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(id, date, currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteOverrideAsync(
        [AsParameters] DeleteShiftOverrideRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationDeleteShiftOverrideRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<DayScheduleResponse>> GetEmployeeDayAsync(
        [AsParameters] GetEmployeeDayScheduleRequest request,
        ClaimsPrincipal user,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetEmployeeDayScheduleRequest, DayScheduleResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(
                currentUser.GetCompanyId(),
                AccessScopes.Resolve(user, CorePermissions.Shifts.Manage),
                idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }
}
