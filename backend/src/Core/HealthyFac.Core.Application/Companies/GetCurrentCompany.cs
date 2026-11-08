using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Companies;

public record GetCurrentCompanyRequest : IRequest<GetCurrentCompanyResponse>;

public record GetCurrentCompanyResponse
{
    public required string Name { get; init; }

    public required string TimeZoneId { get; init; }
}

public sealed class GetCurrentCompanyPreHandlerCheck : PassThroughPreHandlerCheck<GetCurrentCompanyRequest>;

/// <summary>
/// Reads the company of the actor. The handler reads the company from the actor, so it has no scope rule of its own.
/// The soft delete filter hides a deleted company.
/// </summary>
/// <param name="dbContext">Reads the company.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetCurrentCompanyRequestHandler(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<GetCurrentCompanyRequest, GetCurrentCompanyResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<GetCurrentCompanyResponse> HandleAsync(
        GetCurrentCompanyRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var companyId = _currentUser.GetCompanyId();

        var company = await _dbContext.Companies
            .AsNoTracking()
            .Where(candidate => candidate.Id == companyId)
            .Select(candidate => new GetCurrentCompanyResponse
            {
                Name = candidate.Name,
                TimeZoneId = candidate.TimeZoneId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return company ?? throw new NotFoundException(CompanyErrors.NotFound);
    }
}
