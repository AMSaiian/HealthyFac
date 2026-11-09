using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Shared.Application.Grids;
using ApplicationSearchFilesRequest = HealthyFac.Document.Application.Files.Searching.SearchFilesRequest;

namespace HealthyFac.Document.Web.Files;

public record SearchFilesRequest : GridRequest<FileGridFilter, FileSortColumn>;

public static class SearchFilesRequestExtensions
{
    extension(SearchFilesRequest request)
    {
        public ApplicationSearchFilesRequest ToApplicationRequest(Guid companyId, Guid? machineId) =>
            new()
            {
                CompanyId = companyId,
                MachineId = machineId,
                Offset = request.Offset,
                Limit = request.Limit,
                Sort = request.Sort,
                Filter = request.Filter,
            };
    }
}
