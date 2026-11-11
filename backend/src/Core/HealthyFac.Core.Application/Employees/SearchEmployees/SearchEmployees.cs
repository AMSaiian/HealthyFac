using FluentValidation;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.SearchEmployees;

public record SearchEmployeesRequest : GridRequest<EmployeeGridFilter, EmployeeSortColumn>,
    IRequest<GridResponse<EmployeeRowResponse>>
{
    public required Guid CompanyId { get; init; }
}

public record EmployeeRowResponse
{
    public required Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Email { get; init; }

    public required RoleCode RoleCode { get; init; }

    public required EmployeeStatus Status { get; init; }

    public required DateTime CreatedAt { get; init; }
}

public sealed class SearchEmployeesRequestValidator
    : GridRequestValidator<SearchEmployeesRequest, EmployeeGridFilter, EmployeeSortColumn>
{
    public SearchEmployeesRequestValidator()
        : base(EmployeeSortColumn.All)
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Filter)
            .SetValidator(new EmployeeGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchEmployeesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchEmployeesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchEmployeesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the employees of the company. The query always holds the company of the actor, whatever the
/// filter says.
/// </summary>
/// <param name="dbContext">Reads the employees.</param>
public sealed class SearchEmployeesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<SearchEmployeesRequest, GridResponse<EmployeeRowResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<GridResponse<EmployeeRowResponse>> HandleAsync(
        SearchEmployeesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.CompanyId == request.CompanyId)
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort, EmployeeSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                employee => new EmployeeRowResponse
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = employee.Email,
                    RoleCode = employee.Role.Code,
                    Status = employee.Status,
                    CreatedAt = employee.CreatedAt,
                },
                cancellationToken);

        return response;
    }
}
