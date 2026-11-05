using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Departments;

public record ListEmployeeDepartmentsRequest : IRequest<ListEmployeeDepartmentsResponse>;

public record ListEmployeeDepartmentsResponse
{
    public required IReadOnlyList<DepartmentResponse> Items { get; init; }
}

public record DepartmentResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}

public sealed class ListEmployeeDepartmentsPreHandlerCheck : PassThroughPreHandlerCheck<ListEmployeeDepartmentsRequest>;

/// <summary>
/// Lists the departments of the current employee, sorted by name. The handler reads the company and the department
/// IDs from the actor, so it has no scope rule of its own.
/// </summary>
/// <param name="dbContext">Reads the departments.</param>
/// <param name="currentUser">Provides the company and the departments of the actor.</param>
public sealed class ListEmployeeDepartmentsRequestHandler(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<ListEmployeeDepartmentsRequest, ListEmployeeDepartmentsResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<ListEmployeeDepartmentsResponse> HandleAsync(
        ListEmployeeDepartmentsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var companyId = _currentUser.GetCompanyId();
        var departmentIds = _currentUser.GetActor().DepartmentIds.ToList();

        var items = await _dbContext.Departments
            .AsNoTracking()
            .Where(department => department.CompanyId == companyId && departmentIds.Contains(department.Id))
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentResponse { Id = department.Id, Name = department.Name })
            .ToListAsync(cancellationToken);

        return new ListEmployeeDepartmentsResponse { Items = items };
    }
}
