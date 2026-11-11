using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Authorization;

/// <summary>
/// Finds the employee of the actor. A scoped use case of the own scope uses it: an actor without an employee in the
/// company reaches nothing.
/// </summary>
/// <param name="dbContext">Reads the employees.</param>
/// <param name="currentUser">Reads the actor.</param>
public sealed class OwnEmployeeResolver(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    /// <summary>
    /// Finds the ID of the employee whose identity is the actor, inside the company of the actor.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The employee ID, or <see langword="null"/> when the actor has no employee.</returns>
    public async Task<Guid?> FindIdAsync(CancellationToken cancellationToken)
    {
        var companyId = _currentUser.GetCompanyId();
        var userId = _currentUser.GetUserId();

        return await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.CompanyId == companyId && employee.IdentityUserId == userId)
            .Select(employee => (Guid?)employee.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
