using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;

public record SearchStatusDefinitionsRequest : GridRequest<StatusDefinitionGridFilter, StatusDefinitionSortColumn>,
    IRequest<GridResponse<StatusDefinitionResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

public sealed class SearchStatusDefinitionsRequestValidator
    : GridRequestValidator<SearchStatusDefinitionsRequest, StatusDefinitionGridFilter, StatusDefinitionSortColumn>
{
    public SearchStatusDefinitionsRequestValidator()
        : base(StatusDefinitionSortColumn.All)
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.CompanyIdRequired);

        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.MachineIdRequired);

        RuleFor(request => request.Filter)
            .SetValidator(new StatusDefinitionGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class SearchStatusDefinitionsPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<SearchStatusDefinitionsRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(SearchStatusDefinitionsRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(
            request.CompanyId,
            request.MachineId,
            cancellationToken);
    }
}

/// <summary>
/// Reads one page of the status definitions of a machine. The pre-handler check has already checked the machine scope,
/// so a machine outside the scope is "not found" and never an empty page.
/// </summary>
/// <param name="dbContext">Reads the status definitions.</param>
public sealed class SearchStatusDefinitionsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<SearchStatusDefinitionsRequest, GridResponse<StatusDefinitionResponse>>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<GridResponse<StatusDefinitionResponse>> HandleAsync(
        SearchStatusDefinitionsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.StatusDefinitions
            .AsNoTracking()
            .Where(status => status.MachineRegistrationId == request.MachineId)
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort, StatusDefinitionSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                StatusDefinitionResponse.Projection,
                cancellationToken);

        return response;
    }
}
