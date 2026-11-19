using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationCreateOperatingPhaseRequest =
    HealthyFac.Telemetry.Application.OperatingPhases.CreateOperatingPhaseRequest;

namespace HealthyFac.Telemetry.Web.OperatingPhases;

/// <summary>The body of the create call. The company comes from the token and the machine from the route.</summary>
public record CreateOperatingPhaseRequest : OperatingPhaseInput;

public static class CreateOperatingPhaseRequestExtensions
{
    extension(CreateOperatingPhaseRequest request)
    {
        public ApplicationCreateOperatingPhaseRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
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
