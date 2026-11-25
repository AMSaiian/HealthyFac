using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationSearchMachineDraftsRequest =
    HealthyFac.Core.Application.Drafts.SearchMachineDrafts.SearchMachineDraftsRequest;

namespace HealthyFac.Core.Web.Drafts;

public record SearchMachineDraftsRequest : KeysetGridRequest<MachineDraftGridFilter, MachineDraftSortColumn>;

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
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
                After = request.After,
            };

        public ApplicationSearchMachineDraftsRequest ToApplicationChatRequest(
            string encryptedChatId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                ChatId = DraftRouteIds.Decrypt(encryptedChatId, idEncryptor),
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
                After = request.After,
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
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
                After = request.After,
            };
    }
}
