using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.Web.RateLimiting;
using HealthyFac.Shared.Web.CrossOrigin;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>
/// Maps the composed draft endpoints. They answer in the public <c>/api/core</c> space, like the proxied Core
/// routes, but the Gateway calls Core and Telemetry itself and merges the answers.
/// </summary>
public static class DraftCompositionEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the composed draft endpoints. Call it before <c>MapReverseProxy</c>.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapDraftCompositionEndpoints()
        {
            app.MapPost(DraftCompositionRoutes.MachineDraftSearch, SearchMachineDraftsAsync)
                .RequireCors(CorsConstants.PolicyName)
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);
            app.MapPost(DraftCompositionRoutes.AttemptDraftSearch, SearchAttemptDraftsAsync)
                .RequireCors(CorsConstants.PolicyName)
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);
            app.MapGet(DraftCompositionRoutes.AttemptDrafts, GetAttemptDraftsAsync)
                .RequireCors(CorsConstants.PolicyName)
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);
            app.MapPost(DraftCompositionRoutes.MachineDraftDismiss, DismissMachineDraftsAsync)
                .RequireCors(CorsConstants.PolicyName)
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);
            app.MapPost(DraftCompositionRoutes.AttemptDraftDismiss, DismissAttemptDraftsAsync)
                .RequireCors(CorsConstants.PolicyName)
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);

            return app;
        }
    }

    private static async Task<IResult> GetAttemptDraftsAsync(
        [FromRoute(Name = DraftCompositionRoutes.VersionParameter)] string version,
        [FromRoute(Name = DraftCompositionRoutes.AttemptIdParameter)]
        string attemptId,
        HttpContext context,
        AttemptDraftsComposer composer,
        CancellationToken cancellationToken
    ) =>
        await composer.GetAsync(version, attemptId, context, cancellationToken);

    private static async Task<IResult> DismissMachineDraftsAsync(
        [FromRoute(Name = DraftCompositionRoutes.VersionParameter)] string version,
        [FromRoute(Name = DraftCompositionRoutes.MachineIdParameter)]
        string machineId,
        HttpContext context,
        DraftDismissComposer composer,
        CancellationToken cancellationToken
    ) =>
        await composer.DismissAsync(
            DraftCompositionRoutes.CoreMachineDismiss(version, machineId),
            DraftCompositionRoutes.TelemetryMachineDismiss(version, machineId),
            context,
            cancellationToken);

    private static async Task<IResult> DismissAttemptDraftsAsync(
        [FromRoute(Name = DraftCompositionRoutes.VersionParameter)] string version,
        [FromRoute(Name = DraftCompositionRoutes.AttemptIdParameter)]
        string attemptId,
        HttpContext context,
        DraftDismissComposer composer,
        CancellationToken cancellationToken
    ) =>
        await composer.DismissAsync(
            DraftCompositionRoutes.CoreAttemptDismiss(version, attemptId),
            DraftCompositionRoutes.TelemetryAttemptDismiss(version, attemptId),
            context,
            cancellationToken);

    private static async Task<IResult> SearchMachineDraftsAsync(
        [FromRoute(Name = DraftCompositionRoutes.VersionParameter)] string version,
        [FromRoute(Name = DraftCompositionRoutes.MachineIdParameter)]
        string machineId,
        HttpContext context,
        DraftSearchComposer composer,
        CancellationToken cancellationToken
    ) =>
        await composer.SearchAsync(
            new DraftSearchScope { Version = version, MachineId = machineId },
            await ReadBodyAsync(context, cancellationToken),
            context,
            cancellationToken);

    private static async Task<IResult> SearchAttemptDraftsAsync(
        [FromRoute(Name = DraftCompositionRoutes.VersionParameter)] string version,
        [FromRoute(Name = DraftCompositionRoutes.AttemptIdParameter)]
        string attemptId,
        HttpContext context,
        DraftSearchComposer composer,
        CancellationToken cancellationToken
    ) =>
        await composer.SearchAsync(
            new DraftSearchScope { Version = version, AttemptId = attemptId },
            await ReadBodyAsync(context, cancellationToken),
            context,
            cancellationToken);

    private static async Task<JsonObject?> ReadBodyAsync(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<JsonObject>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
