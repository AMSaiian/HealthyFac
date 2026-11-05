using HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;
using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationSearchMachineErrorCodesRequest =
    HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes.SearchMachineErrorCodesRequest;

namespace HealthyFac.Core.Web.MachineErrorCodes;

public record SearchMachineErrorCodesRequest : GridRequest<MachineErrorCodeGridFilter, MachineErrorCodeSortColumn>;

public static class SearchMachineErrorCodesRequestExtensions
{
    extension(SearchMachineErrorCodesRequest request)
    {
        public ApplicationSearchMachineErrorCodesRequest ToApplicationRequest(
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
