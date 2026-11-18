using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.Web.Composition.Drafts;
using HealthyFac.Gateway.Web.RateLimiting;
using HealthyFac.Shared.Web.CrossOrigin;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Gateway.Web.Composition.Machines;

/// <summary>
/// Maps the composed machine endpoint: the machine grid of Core with the pending draft counts of Telemetry.
/// </summary>
public static class MachineCompositionEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the composed machine search. Call it before <c>MapReverseProxy</c>.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapMachineCompositionEndpoints()
        {
            app.MapPost(MachineCompositionRoutes.MachineSearch, SearchMachinesAsync)
                .RequireCors(CorsConstants.PolicyName)
                .RequireAuthorization(GatewayConstants.AuthenticatedPolicyName)
                .RequireRateLimiting(RateLimitPolicyNames.Default);

            return app;
        }
    }

    private static async Task<IResult> SearchMachinesAsync(
        [FromRoute(Name = DraftCompositionRoutes.VersionParameter)] string version,
        HttpContext context,
        MachineSearchComposer composer,
        CancellationToken cancellationToken
    )
    {
        JsonObject? body;

        try
        {
            body = await context.Request.ReadFromJsonAsync<JsonObject>(cancellationToken);
        }
        catch (JsonException)
        {
            body = null;
        }

        return await composer.SearchAsync(version, body, context, cancellationToken);
    }
}
