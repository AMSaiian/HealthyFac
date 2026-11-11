using ApplicationMarkDraftsAppliedRequest = HealthyFac.Core.Application.Drafts.MarkDraftsAppliedRequest;

namespace HealthyFac.Core.Web.Drafts;

public record MarkDraftsAppliedRequest
{
    public required IReadOnlyList<Guid> Ids { get; init; }
}

public static class MarkDraftsAppliedRequestExtensions
{
    extension(MarkDraftsAppliedRequest request)
    {
        public ApplicationMarkDraftsAppliedRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, Ids = request.Ids };
    }
}
