using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;

/// <summary>
/// Searches the definition drafts in one scope: the drafts that target one machine, or the drafts of one ingest
/// attempt. Set exactly one of <see cref="MachineId"/> and <see cref="AttemptId"/>.
/// </summary>
public record SearchDefinitionDraftsRequest : KeysetGridRequest<DefinitionDraftGridFilter, DefinitionDraftSortColumn>,
    IRequest<KeysetGridResponse<DefinitionDraftGridItem>>
{
    public required Guid CompanyId { get; init; }

    public Guid? MachineId { get; init; }

    public Guid? AttemptId { get; init; }
}

public sealed class SearchDefinitionDraftsRequestValidator
    : KeysetGridRequestValidator<SearchDefinitionDraftsRequest, DefinitionDraftGridFilter, DefinitionDraftSortColumn>
{
    public SearchDefinitionDraftsRequestValidator()
        : base(DefinitionDraftSortColumn.All)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);
        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(DefinitionDraftErrors.MachineIdRequired)
            .When(request => request.MachineId is not null);
        RuleFor(request => request.AttemptId)
            .NotEmpty()
            .WithMessage(DefinitionDraftErrors.AttemptIdRequired)
            .When(request => request.AttemptId is not null);
        RuleFor(request => request)
            .Must(request => request.MachineId is null != request.AttemptId is null)
            .WithMessage(DefinitionDraftErrors.SearchScopeRequired);
        RuleFor(request => request.Filter)
            .SetValidator(new DefinitionDraftGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

/// <summary>
/// Checks the company of the actor, then, for a machine scope, that the machine is an active registration of the
/// company (else "not found"). An attempt of another company has no rows in the scope of the company, so it needs no
/// check.
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class SearchDefinitionDraftsPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<SearchDefinitionDraftsRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(SearchDefinitionDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Reads one page of the metric and status drafts in the scope, in one query. The pending drafts come first unless
/// the request names a sort.
/// </summary>
/// <param name="dbContext">Reads the drafts.</param>
public sealed class SearchDefinitionDraftsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<SearchDefinitionDraftsRequest, KeysetGridResponse<DefinitionDraftGridItem>>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<KeysetGridResponse<DefinitionDraftGridItem>> HandleAsync(
        SearchDefinitionDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await DefinitionDraftGridItem
            .Union(
                _dbContext.MetricDefinitionDrafts.AsNoTracking(),
                _dbContext.StatusDefinitionDrafts.AsNoTracking(),
                request.CompanyId,
                request.MachineId,
                request.AttemptId)
            .ApplyFilter(request.Filter)
            .ToKeysetResponseAsync(
                request.Sort ?? DefinitionDraftSortMap.Default,
                DefinitionDraftSortMap.Instance,
                request.Limit,
                request.After,
                cancellationToken);

        return response;
    }
}
