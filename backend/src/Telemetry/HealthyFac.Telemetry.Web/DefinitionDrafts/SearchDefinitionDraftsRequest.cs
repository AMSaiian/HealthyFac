using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationSearchDefinitionDraftsRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts.SearchDefinitionDraftsRequest;

namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

public record SearchDefinitionDraftsRequest : KeysetGridRequest<DefinitionDraftGridFilter, DefinitionDraftSortColumn>;

public static class SearchDefinitionDraftsRequestExtensions
{
    extension(SearchDefinitionDraftsRequest request)
    {
        public ApplicationSearchDefinitionDraftsRequest ToApplicationRequest(
            string encryptedMachineId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
                After = request.After,
            };

        public ApplicationSearchDefinitionDraftsRequest ToApplicationAttemptRequest(
            string encryptedAttemptId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                AttemptId = DefinitionDraftRouteIds.Decrypt(encryptedAttemptId, idEncryptor),
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
                After = request.After,
            };
    }
}
