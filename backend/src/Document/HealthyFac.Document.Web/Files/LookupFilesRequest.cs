using HealthyFac.Shared.Application.IdEncryption;
using ApplicationLookupFilesRequest = HealthyFac.Document.Application.Files.Lookup.LookupFilesRequest;

namespace HealthyFac.Document.Web.Files;

public record LookupFilesRequest
{
    [IgnoreIdEncryption]
    public required IReadOnlyList<string> Ids { get; init; }
}

public static class LookupFilesRequestExtensions
{
    extension(LookupFilesRequest request)
    {
        public ApplicationLookupFilesRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Ids = request.Ids };
    }
}
