using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines.SearchMachines;

public record SearchMachinesRequest : GridRequest<MachineGridFilter, MachineSortColumn>,
    IRequest<GridResponse<MachineResponse>>
{
    public required Guid CompanyId { get; init; }
}

public sealed class SearchMachinesRequestValidator
    : GridRequestValidator<SearchMachinesRequest, MachineGridFilter, MachineSortColumn>
{
    public SearchMachinesRequestValidator()
        : base(MachineSortColumn.All)
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(request => request.Filter)
            .SetValidator(new MachineGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchMachinesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchMachinesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchMachinesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        if (request.Filter.DepartmentIds is not null)
        {
            actor.EnsureDepartments(request.Filter.DepartmentIds.Ids);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the machines of the actor. The query always holds the company and the departments of the actor,
/// whatever the filter says. A filter on departments can only narrow the result.
/// </summary>
/// <param name="dbContext">Reads the machines.</param>
/// <param name="currentUser">Provides the departments of the actor.</param>
public sealed class SearchMachinesRequestHandler(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<SearchMachinesRequest, GridResponse<MachineResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<GridResponse<MachineResponse>> HandleAsync(
        SearchMachinesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var departmentIds = _currentUser.GetActor().DepartmentIds.ToList();

        var response = await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machine.CompanyId == request.CompanyId && departmentIds.Contains(machine.DepartmentId))
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort, MachineSortMap.Instance)
            .ToGridResponseAsync(request.Offset, request.Limit, MachineResponse.Projection, cancellationToken);

        return response;
    }
}
