using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationReorderOperatingPhasesRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.ReorderOperatingPhasesRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

/// <summary>The body of the reorder call: the IDs of all phases of the machine in the new order.</summary>
public record ReorderOperatingPhasesRequest
{
    public required IReadOnlyList<Guid> PhaseIds { get; init; }
}

public static class ReorderOperatingPhasesRequestExtensions
{
    extension(ReorderOperatingPhasesRequest request)
    {
        public ApplicationReorderOperatingPhasesRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                PhaseIds = request.PhaseIds,
            };
    }
}
