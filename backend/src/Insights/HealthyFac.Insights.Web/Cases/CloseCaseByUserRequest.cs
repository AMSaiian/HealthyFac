using ApplicationCloseCaseByUserRequest = HealthyFac.Insights.Application.Cases.CloseCaseByUserRequest;

namespace HealthyFac.Insights.Web.Cases;

/// <summary>
/// The body of the close of a case. The case and the company come from the route and the token.
/// </summary>
public record CloseCaseByUserRequest
{
    public required string Reason { get; init; }
}

public static class CloseCaseByUserRequestExtensions
{
    extension(CloseCaseByUserRequest request)
    {
        public ApplicationCloseCaseByUserRequest ToApplicationRequest(Guid companyId, Guid id) =>
            new()
            {
                CompanyId = companyId,
                Id = id,
                Reason = request.Reason,
            };
    }
}
