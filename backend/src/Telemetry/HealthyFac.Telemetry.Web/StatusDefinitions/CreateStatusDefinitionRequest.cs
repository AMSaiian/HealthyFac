using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationCreateStatusDefinitionRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.CreateStatusDefinitionRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

public record CreateStatusDefinitionRequest
{
    public required string Code { get; init; }
    public required StatusSeverity? Severity { get; init; }
    public string? Description { get; init; }
}

public static class CreateStatusDefinitionRequestExtensions
{
    extension(CreateStatusDefinitionRequest request)
    {
        public ApplicationCreateStatusDefinitionRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Code = request.Code,
                Severity = request.Severity,
                Description = request.Description,
            };
    }
}
