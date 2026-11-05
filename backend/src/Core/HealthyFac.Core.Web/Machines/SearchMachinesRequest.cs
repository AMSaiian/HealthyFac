using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Web.Employees;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchMachinesRequest = HealthyFac.Core.Application.Machines.SearchMachines.SearchMachinesRequest;

namespace HealthyFac.Core.Web.Machines;

public record SearchMachinesRequest : GridRequest<MachineGridFilter, MachineSortColumn>;

public static class SearchMachinesRequestExtensions
{
    extension(SearchMachinesRequest request)
    {
        public ApplicationSearchMachinesRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
            };
    }
}
