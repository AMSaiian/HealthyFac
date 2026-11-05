using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;

public record SearchMachineErrorCodesRequest : GridRequest<MachineErrorCodeGridFilter, MachineErrorCodeSortColumn>,
    IRequest<GridResponse<MachineErrorCodeResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

public sealed class SearchMachineErrorCodesRequestValidator
    : GridRequestValidator<SearchMachineErrorCodesRequest, MachineErrorCodeGridFilter, MachineErrorCodeSortColumn>
{
    public SearchMachineErrorCodesRequestValidator()
        : base(MachineErrorCodeSortColumn.All)
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.CompanyIdRequired);

        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.MachineIdRequired);

        RuleFor(request => request.Filter)
            .SetValidator(new MachineErrorCodeGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company or the departments of the actor, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company and the departments of the actor.</param>
public sealed class SearchMachineErrorCodesPreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchMachineErrorCodesRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(SearchMachineErrorCodesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(
            request.CompanyId,
            actor.DepartmentIds.ToList(),
            request.MachineId,
            cancellationToken);
    }
}

/// <summary>
/// Reads one page of the error codes of a machine. The pre-handler check has already checked the machine scope, so a
/// machine outside the scope is "not found" and never an empty page.
/// </summary>
/// <param name="dbContext">Reads the error codes.</param>
public sealed class SearchMachineErrorCodesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<SearchMachineErrorCodesRequest, GridResponse<MachineErrorCodeResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<GridResponse<MachineErrorCodeResponse>> HandleAsync(
        SearchMachineErrorCodesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.MachineErrorCodes
            .AsNoTracking()
            .Where(errorCode => errorCode.MachineId == request.MachineId)
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort, MachineErrorCodeSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                MachineErrorCodeResponse.Projection,
                cancellationToken);

        return response;
    }
}
