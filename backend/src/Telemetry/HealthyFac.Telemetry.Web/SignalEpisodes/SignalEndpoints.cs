using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Application.SignalEpisodes;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using ApplicationGetPhaseTimesRequest = HealthyFac.Telemetry.Application.OperatingPhases.GetPhaseTimesRequest;
using ApplicationGetMetricHistoryRequest =
    HealthyFac.Telemetry.Application.SignalEpisodes.GetMetricHistoryRequest;
using ApplicationGetSignalEpisodesRequest =
    HealthyFac.Telemetry.Application.SignalEpisodes.GetSignalEpisodesRequest;

namespace HealthyFac.Telemetry.Web.SignalEpisodes;

/// <summary>
/// Configures endpoint routing for the signals of a machine: the metric history, the signal episodes, and the time in
/// each phase.
/// </summary>
public static class SignalEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the signal read endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapSignalEndpoints()
        {
            var group = app.MapGroup(SignalRoutes.MachineRoute);
            group.MapGet(SignalRoutes.MetricHistory, GetMetricHistoryAsync)
                .RequireAuthorization(TelemetryPermissions.Signals.Read);
            group.MapGet(SignalRoutes.SignalEpisodes, GetSignalEpisodesAsync)
                .RequireAuthorization(TelemetryPermissions.Signals.Read);
            group.MapGet(SignalRoutes.PhaseTimes, GetPhaseTimesAsync)
                .RequireAuthorization(TelemetryPermissions.Signals.Read);

            return app;
        }
    }

    private static async Task<Ok<MetricHistoryResponse>> GetMetricHistoryAsync(
        [AsParameters] GetMetricHistoryRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetMetricHistoryRequest, MetricHistoryResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<PhaseTimesResponse>> GetPhaseTimesAsync(
        [AsParameters] GetPhaseTimesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetPhaseTimesRequest, PhaseTimesResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<SignalEpisodesResponse>> GetSignalEpisodesAsync(
        [AsParameters] GetSignalEpisodesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetSignalEpisodesRequest, SignalEpisodesResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }
}
