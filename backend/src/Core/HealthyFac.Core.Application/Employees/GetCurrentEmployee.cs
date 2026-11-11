using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record GetCurrentEmployeeRequest : IRequest<CurrentEmployeeResponse>;

public record CurrentEmployeeResponse
{
    public required Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required RoleCode RoleCode { get; init; }
}

public sealed class GetCurrentEmployeePreHandlerCheck : PassThroughPreHandlerCheck<GetCurrentEmployeeRequest>;

/// <summary>
/// Reads the employee whose identity is the actor, inside the company of the actor. An actor without an employee gets
/// "not found".
/// </summary>
/// <param name="dbContext">Reads the employee.</param>
/// <param name="currentUser">Reads the actor.</param>
public sealed class GetCurrentEmployeeRequestHandler(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<GetCurrentEmployeeRequest, CurrentEmployeeResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<CurrentEmployeeResponse> HandleAsync(
        GetCurrentEmployeeRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var companyId = _currentUser.GetCompanyId();
        var userId = _currentUser.GetUserId();

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Where(candidate => candidate.CompanyId == companyId && candidate.IdentityUserId == userId)
            .Select(candidate => new CurrentEmployeeResponse
            {
                Id = candidate.Id,
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                RoleCode = candidate.Role.Code,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return employee ?? throw new NotFoundException(EmployeeProvisioningErrors.Rejections.EmployeeNotFound);
    }
}
