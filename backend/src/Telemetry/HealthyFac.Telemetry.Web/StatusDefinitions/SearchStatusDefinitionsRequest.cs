using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationSearchStatusDefinitionsRequest =
    HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions.SearchStatusDefinitionsRequest;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

public record SearchStatusDefinitionsRequest : GridRequest<StatusDefinitionGridFilter, StatusDefinitionSortColumn>;

public static class SearchStatusDefinitionsRequestExtensions
{
    extension(SearchStatusDefinitionsRequest request)
    {
        public ApplicationSearchStatusDefinitionsRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
            };
    }
}
