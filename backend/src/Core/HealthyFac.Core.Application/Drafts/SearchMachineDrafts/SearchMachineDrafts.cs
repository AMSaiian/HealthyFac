using FluentValidation;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// Searches the drafts in one scope: the drafts that target one machine, or the drafts of one ingest attempt. Set
/// exactly one of <see cref="MachineId"/> and <see cref="AttemptId"/>.
/// </summary>
public record SearchMachineDraftsRequest : KeysetGridRequest<MachineDraftGridFilter, MachineDraftSortColumn>,
    IRequest<KeysetGridResponse<MachineDraftGridItem>>
{
    public required Guid CompanyId { get; init; }

    public Guid? MachineId { get; init; }

    public Guid? AttemptId { get; init; }
}

public sealed class SearchMachineDraftsRequestValidator
    : KeysetGridRequestValidator<SearchMachineDraftsRequest, MachineDraftGridFilter, MachineDraftSortColumn>
{
    public SearchMachineDraftsRequestValidator()
        : base(MachineDraftSortColumn.All)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);
        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(MachineErrors.MachineIdRequired)
            .When(request => request.MachineId is not null);
        RuleFor(request => request.AttemptId)
            .NotEmpty()
            .WithMessage(DraftErrors.AttemptIdRequired)
            .When(request => request.AttemptId is not null);
        RuleFor(request => request)
            .Must(request => request.MachineId is null != request.AttemptId is null)
            .WithMessage(DraftErrors.SearchScopeRequired);
        RuleFor(request => request.Filter!)
            .SetValidator(new MachineDraftGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

/// <summary>
/// Checks the company of the actor, then that the machine is an active machine of the company (else "not found").
/// An attempt of another company has no rows in the scope of the company, so it needs no check.
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class SearchMachineDraftsPreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchMachineDraftsRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(SearchMachineDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is null)
        {
            return;
        }

        var isMachineInScope = await _dbContext.Machines.AnyAsync(
            machine => machine.Id == request.MachineId && machine.CompanyId == request.CompanyId,
            cancellationToken);

        if (!isMachineInScope)
        {
            throw new NotFoundException(MachineErrors.MachineNotFound);
        }
    }
}

/// <summary>
/// Reads one page of the machine drafts in the scope. The pending drafts come first unless the request names a sort.
/// </summary>
/// <param name="dbContext">Reads the drafts.</param>
public sealed class SearchMachineDraftsRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<SearchMachineDraftsRequest, KeysetGridResponse<MachineDraftGridItem>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<KeysetGridResponse<MachineDraftGridItem>> HandleAsync(
        SearchMachineDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await MachineDraftGridItem
            .Query(
                _dbContext.MachineDrafts.AsNoTracking(),
                _dbContext.Machines.AsNoTracking(),
                request.CompanyId,
                request.MachineId,
                request.AttemptId)
            .ApplyFilter(request.Filter)
            .ToKeysetResponseAsync(
                request.Sort ?? MachineDraftSortMap.Default,
                MachineDraftSortMap.Instance,
                request.Limit,
                request.After,
                cancellationToken);

        return response;
    }
}
