using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetMetricHistoryRequest =
    HealthyFac.Telemetry.Application.SignalEpisodes.GetMetricHistoryRequest;

namespace HealthyFac.Telemetry.Web.SignalEpisodes;

public record GetMetricHistoryRequest
{
    [FromRoute(Name = SignalRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromQuery(Name = SignalRoutes.MetricDefinitionIdParameter)]
    public required string MetricDefinitionId { get; init; }

    [FromQuery(Name = SignalRoutes.FromParameter)]
    public required DateTimeOffset From { get; init; }

    [FromQuery(Name = SignalRoutes.ToParameter)]
    public required DateTimeOffset To { get; init; }

    [FromQuery(Name = SignalRoutes.BucketCountParameter)]
    public required int BucketCount { get; init; }
}

public static class GetMetricHistoryRequestExtensions
{
    extension(GetMetricHistoryRequest request)
    {
        /// <summary>
        /// Builds the Application request. An ID that is not a valid token names nothing, so it gets the same answer
        /// as an unknown machine or metric.
        /// </summary>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="idEncryptor">Decrypts the IDs.</param>
        /// <returns>The Application request.</returns>
        /// <exception cref="NotFoundException">Thrown when an ID is not a valid token.</exception>
        public ApplicationGetMetricHistoryRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                MetricDefinitionId = MachineRouteIds.Decrypt(request.MetricDefinitionId, idEncryptor),
                From = request.From.UtcDateTime,
                To = request.To.UtcDateTime,
                BucketCount = request.BucketCount,
            };
    }
}
