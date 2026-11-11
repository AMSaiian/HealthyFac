using ApplicationLookupMachinesRequest = HealthyFac.Core.Application.Machines.LookupMachinesRequest;

namespace HealthyFac.Core.Web.Machines;

public record LookupMachinesRequest
{
    public string? Search { get; init; }
}

public static class LookupMachinesRequestExtensions
{
    extension(LookupMachinesRequest request)
    {
        public ApplicationLookupMachinesRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Search = request.Search };
    }
}
