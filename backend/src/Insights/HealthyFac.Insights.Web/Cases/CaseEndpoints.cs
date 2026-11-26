using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationCloseCaseByUserRequest = HealthyFac.Insights.Application.Cases.CloseCaseByUserRequest;
using ApplicationSearchCasesRequest = HealthyFac.Insights.Application.Cases.SearchCasesRequest;

namespace HealthyFac.Insights.Web.Cases;

/// <summary>
/// Configures endpoint routing for the maintenance cases. Reading needs <c>cases:read</c>. Running a case again and
/// closing it need <c>cases:write</c>.
/// </summary>
public static class CaseEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps all case endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapCaseEndpoints()
        {
            var group = app.MapGroup(CaseRoutes.BaseRoute);
            group.MapPost(CaseRoutes.Search, SearchAsync).RequireAuthorization(InsightsPermissions.Cases.Read);
            group.MapGet(CaseRoutes.ById, GetAsync).RequireAuthorization(InsightsPermissions.Cases.Read);

            group.MapPost(CaseRoutes.SignalsSearch, SearchSignalsAsync)
                .RequireAuthorization(InsightsPermissions.Cases.Read);

            group.MapPost(CaseRoutes.RunsSearch, SearchRunsAsync).RequireAuthorization(InsightsPermissions.Cases.Read);

            group.MapGet(CaseRoutes.RunToolCalls, GetToolCallsAsync)
                .RequireAuthorization(InsightsPermissions.Cases.Read);

            group.MapPost(CaseRoutes.Runs, RunAgainAsync).RequireAuthorization(InsightsPermissions.Cases.Write);
            group.MapPost(CaseRoutes.Close, CloseAsync).RequireAuthorization(InsightsPermissions.Cases.Write);

            return app;
        }
    }

    private static async Task<Ok<GridResponse<CaseListItemResponse>>> SearchAsync(
        SearchCasesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchCasesRequest, GridResponse<CaseListItemResponse>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<CaseDetailResponse>> GetAsync(
        [FromRoute(Name = CaseRoutes.IdParameter)] string id,
        ICurrentUserProvider currentUser,
        IRequestHandler<GetCaseRequest, CaseDetailResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new GetCaseRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                Id = CaseRouteIds.Decrypt(id, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GridResponse<CaseSignalResponse>>> SearchSignalsAsync(
        [FromRoute(Name = CaseRoutes.IdParameter)] string id,
        SearchCaseChildrenRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<SearchCaseSignalsRequest, GridResponse<CaseSignalResponse>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new SearchCaseSignalsRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                CaseId = CaseRouteIds.Decrypt(id, idEncryptor),
                Offset = request.Offset,
                Limit = request.Limit,
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<GridResponse<CaseRunResponse>>> SearchRunsAsync(
        [FromRoute(Name = CaseRoutes.IdParameter)] string id,
        SearchCaseChildrenRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<SearchCaseRunsRequest, GridResponse<CaseRunResponse>> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new SearchCaseRunsRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                CaseId = CaseRouteIds.Decrypt(id, idEncryptor),
                Offset = request.Offset,
                Limit = request.Limit,
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<CaseToolCallsResponse>> GetToolCallsAsync(
        [FromRoute(Name = CaseRoutes.IdParameter)] string id,
        [FromRoute(Name = CaseRoutes.RunIdParameter)]
        string runId,
        ICurrentUserProvider currentUser,
        IRequestHandler<GetRunToolCallsRequest, CaseToolCallsResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new GetRunToolCallsRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                CaseId = CaseRouteIds.Decrypt(id, idEncryptor),
                RunId = CaseRouteIds.Decrypt(runId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> RunAgainAsync(
        [FromRoute(Name = CaseRoutes.IdParameter)] string id,
        ICurrentUserProvider currentUser,
        IRequestHandler<RunCaseAgainRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            new RunCaseAgainRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                Id = CaseRouteIds.Decrypt(id, idEncryptor),
            },
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> CloseAsync(
        [FromRoute(Name = CaseRoutes.IdParameter)] string id,
        CloseCaseByUserRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCloseCaseByUserRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), CaseRouteIds.Decrypt(id, idEncryptor)),
            cancellationToken);

        return TypedResults.NoContent();
    }
}
