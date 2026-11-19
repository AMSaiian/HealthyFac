using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationUpdateOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.UpdateOperatingPhaseRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

/// <summary>
/// The body of the update call. The company comes from the token, the machine and the phase from the route.
/// </summary>
public record UpdateOperatingPhaseRequest : OperatingPhaseInput;

public static class UpdateOperatingPhaseRequestExtensions
{
    extension(UpdateOperatingPhaseRequest request)
    {
        public ApplicationUpdateOperatingPhaseRequest ToApplicationRequest(
            string encryptedMachineId,
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Id = OperatingPhaseRouteIds.Decrypt(encryptedId, idEncryptor),
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Priority = request.Priority,
                IsTracked = request.IsTracked,
                Conditions = request.Conditions,
                Limits = request.Limits,
            };
    }
}
