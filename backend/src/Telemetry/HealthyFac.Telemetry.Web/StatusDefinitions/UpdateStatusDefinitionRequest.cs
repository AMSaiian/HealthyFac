using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationUpdateStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.UpdateStatusDefinitionRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

public record UpdateStatusDefinitionRequest
{
    public required string Code { get; init; }
    public required StatusSeverity? Severity { get; init; }
    public string? Description { get; init; }
}

public static class UpdateStatusDefinitionRequestExtensions
{
    extension(UpdateStatusDefinitionRequest request)
    {
        public ApplicationUpdateStatusDefinitionRequest ToApplicationRequest(
            string encryptedMachineId,
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Id = StatusDefinitionRouteIds.Decrypt(encryptedId, idEncryptor),
                Code = request.Code,
                Severity = request.Severity,
                Description = request.Description,
            };
    }
}
