using HealthyFac.Shared.Application.IdEncryption;
using ApplicationGetFileLinksRequest = HealthyFac.Document.Application.Files.Links.GetFileLinksRequest;

namespace HealthyFac.Document.Web.Files;

public record GetFileLinksRequest
{
    [IgnoreIdEncryption]
    public required IReadOnlyList<string> Ids { get; init; }
}

public static class GetFileLinksRequestExtensions
{
    extension(GetFileLinksRequest request)
    {
        public ApplicationGetFileLinksRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Ids = request.Ids };
    }
}
