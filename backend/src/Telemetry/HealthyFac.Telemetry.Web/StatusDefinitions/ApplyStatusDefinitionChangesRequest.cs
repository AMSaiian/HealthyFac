using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationApplyStatusDefinitionChangesRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.ApplyStatusDefinitionChangesRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

/// <summary>The body of the batch. The company and the machine come from the token and the route.</summary>
public record ApplyStatusDefinitionChangesRequest
{
    public IReadOnlyList<StatusDefinitionChange> Items { get; init; } = [];
}

public static class ApplyStatusDefinitionChangesRequestExtensions
{
    extension(ApplyStatusDefinitionChangesRequest request)
    {
        public ApplicationApplyStatusDefinitionChangesRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Items = request.Items,
            };
    }
}
