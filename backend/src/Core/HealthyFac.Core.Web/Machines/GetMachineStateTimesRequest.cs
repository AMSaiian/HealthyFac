using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetMachineStateTimesRequest = HealthyFac.Core.Application.Machines.States.GetMachineStateTimesRequest;

namespace HealthyFac.Core.Web.Machines;

public record GetMachineStateTimesRequest
{
    [FromRoute(Name = MachineRoutes.IdParameter)]
    public required string Id { get; init; }

    [FromQuery(Name = MachineRoutes.FromParameter)]
    public required DateTimeOffset From { get; init; }

    [FromQuery(Name = MachineRoutes.ToParameter)]
    public required DateTimeOffset To { get; init; }
}

public static class GetMachineStateTimesRequestExtensions
{
    extension(GetMachineStateTimesRequest request)
    {
        public ApplicationGetMachineStateTimesRequest ToApplicationRequest(
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.Id, idEncryptor),
                From = request.From.UtcDateTime,
                To = request.To.UtcDateTime,
            };
    }
}
