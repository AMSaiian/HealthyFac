using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetPhaseTimesRequest = HealthyFac.Telemetry.Application.OperatingPhases.GetPhaseTimesRequest;

namespace HealthyFac.Telemetry.Web.SignalEpisodes;

public record GetPhaseTimesRequest
{
    [FromRoute(Name = SignalRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }

    [FromQuery(Name = SignalRoutes.FromParameter)]
    public required DateTimeOffset From { get; init; }

    [FromQuery(Name = SignalRoutes.ToParameter)]
    public required DateTimeOffset To { get; init; }
}

public static class GetPhaseTimesRequestExtensions
{
    extension(GetPhaseTimesRequest request)
    {
        /// <summary>
        /// Builds the Application request.
        /// </summary>
        /// <param name="companyId">The company of the actor.</param>
        /// <param name="idEncryptor">Decrypts the machine ID.</param>
        /// <returns>The Application request.</returns>
        /// <exception cref="NotFoundException">Thrown when the machine ID is not a valid token.</exception>
        public ApplicationGetPhaseTimesRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                From = request.From.UtcDateTime,
                To = request.To.UtcDateTime,
            };
    }
}
