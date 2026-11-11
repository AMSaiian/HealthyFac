using FluentValidation;
using HealthyFac.Core.Domain.Machines;
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

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the machines of the actor. The query always holds the company of the actor,
/// whatever the filter says.
/// </summary>
/// <param name="dbContext">Reads the machines.</param>
public sealed class SearchMachinesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<SearchMachinesRequest, GridResponse<MachineResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<GridResponse<MachineResponse>> HandleAsync(
        SearchMachinesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machine.CompanyId == request.CompanyId)
            .ApplyFilter(request.Filter)
            .ApplyPendingDraftsFilter(_dbContext, request.Filter.HasPendingDrafts)
            .ApplySort(request.Sort, MachineSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                MachineResponse.CreateProjection(_dbContext),
                cancellationToken);

        return response;
    }
}

internal static class MachineGridQueryExtensions
{
    extension(IQueryable<Machine> query)
    {
        public IQueryable<Machine> ApplyPendingDraftsFilter(ICoreDbContext dbContext, bool? hasPendingDrafts) =>
            hasPendingDrafts is { } flag ? query.Where(MachinePendingDrafts.Predicate(dbContext, flag)) : query;
    }
}
