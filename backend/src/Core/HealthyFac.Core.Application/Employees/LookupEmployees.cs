using FluentValidation;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record LookupEmployeesRequest : IRequest<LookupResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>Part of the first or last name. Without it the lookup answers the first employees by name.</summary>
    public string? Search { get; init; }
}

public sealed class LookupEmployeesRequestValidator : AbstractValidator<LookupEmployeesRequest>
{
    public LookupEmployeesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(LookupErrors.CompanyIdRequired);

        RuleFor(request => request.Search)
            .MaximumLength(LookupValidationConstants.SearchMaxLength)
            .WithMessage(LookupErrors.SearchTooLong)
            .SingleLineText();
    }
}

public sealed class LookupEmployeesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<LookupEmployeesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(LookupEmployeesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the ID and the name (first and last name) of the active employees of the company for a picker or a filter,
/// at most <see cref="LookupValidationConstants.MaxItems"/>, sorted by name.
/// </summary>
/// <param name="dbContext">Reads the employees.</param>
public sealed class LookupEmployeesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<LookupEmployeesRequest, LookupResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<LookupResponse> HandleAsync(
        LookupEmployeesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var query = _dbContext.Employees
            .Where(employee => employee.CompanyId == request.CompanyId && employee.Status == EmployeeStatus.Active);
        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(employee => (employee.FirstName + " " + employee.LastName).Contains(request.Search));
        }

        var items = await query
            .OrderBy(employee => employee.FirstName)
            .ThenBy(employee => employee.LastName)
            .ThenBy(employee => employee.Id)
            .Take(LookupValidationConstants.MaxItems)
            .Select(employee => new LookupOption
            {
                Id = employee.Id,
                DisplayName = employee.FirstName + " " + employee.LastName,
            })
            .ToListAsync(cancellationToken);

        return new LookupResponse { Items = items };
    }
}
