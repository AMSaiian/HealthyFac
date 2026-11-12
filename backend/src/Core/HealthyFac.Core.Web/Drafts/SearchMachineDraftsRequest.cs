using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationSearchMachineDraftsRequest =
    HealthyFac.Core.Application.Drafts.SearchMachineDrafts.SearchMachineDraftsRequest;

namespace HealthyFac.Core.Web.Drafts;

public record SearchMachineDraftsRequest : GridRequest<MachineDraftGridFilter, MachineDraftSortColumn>;

public static class SearchMachineDraftsRequestExtensions
{
    extension(SearchMachineDraftsRequest request)
    {
        public ApplicationSearchMachineDraftsRequest ToApplicationRequest(
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

        public ApplicationSearchMachineDraftsRequest ToApplicationAttemptRequest(
            string encryptedAttemptId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                AttemptId = DraftRouteIds.Decrypt(encryptedAttemptId, idEncryptor),
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
            };
    }
}
