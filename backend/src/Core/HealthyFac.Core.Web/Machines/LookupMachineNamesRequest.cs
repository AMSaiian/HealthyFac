using ApplicationLookupMachineNamesRequest = HealthyFac.Core.Application.Machines.Lookups.LookupMachineNamesRequest;

namespace HealthyFac.Core.Web.Machines;

public record LookupMachineNamesRequest
{
    public required IReadOnlyList<Guid> MachineIds { get; init; }

    public required IReadOnlyList<Guid> MachineDraftIds { get; init; }
}

public static class LookupMachineNamesRequestExtensions
{
    extension(LookupMachineNamesRequest request)
    {
        public ApplicationLookupMachineNamesRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                MachineIds = request.MachineIds,
                MachineDraftIds = request.MachineDraftIds,
            };
    }
}
