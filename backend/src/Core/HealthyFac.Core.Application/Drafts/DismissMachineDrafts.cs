using FluentValidation;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record DismissMachineDraftsRequest : IRequest<DismissMachineDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

public record DismissMachineDraftsResponse
{
    public required int DismissedCount { get; init; }
}

public class DismissMachineDraftsRequestValidator : AbstractValidator<DismissMachineDraftsRequest>
{
    public DismissMachineDraftsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);
        RuleFor(request => request.MachineId).NotEmpty().WithMessage(MachineErrors.MachineIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor and that the machine belongs to it. A machine of another company is "not found".
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DismissMachineDraftsPreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DismissMachineDraftsRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DismissMachineDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

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
/// Dismisses every pending draft that targets an existing machine of the company: the update machine draft, the
/// metric drafts, and the status drafts. The drafts that target a create machine draft are not touched; they belong to
/// a new machine. One save stores all changes, so a draft that changed in parallel is a conflict and nothing is
/// dismissed.
/// </summary>
/// <param name="dbContext">Loads the drafts and saves the change.</param>
public sealed class DismissMachineDraftsRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<DismissMachineDraftsRequest, DismissMachineDraftsResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    /// <summary>
    /// Loads the pending drafts of the machine, dismisses them, and saves once.
    /// </summary>
    /// <param name="request">The company and the machine.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The number of dismissed drafts.</returns>
    /// <exception cref="ConflictException">Thrown when a draft changed while the use case ran.</exception>
    public async Task<DismissMachineDraftsResponse> HandleAsync(
        DismissMachineDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var machineDrafts = await _dbContext.MachineDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && draft.TargetMachineId == request.MachineId
                && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);
        var metricDrafts = await _dbContext.MetricDefinitionDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && draft.TargetMachineId == request.MachineId
                && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);
        var statusDrafts = await _dbContext.StatusDefinitionDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && draft.TargetMachineId == request.MachineId
                && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);

        machineDrafts.ForEach(draft => draft.Dismiss());
        metricDrafts.ForEach(draft => draft.Dismiss());
        statusDrafts.ForEach(draft => draft.Dismiss());

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DraftErrors.DraftChanged, exception);
        }

        return new DismissMachineDraftsResponse
        {
            DismissedCount = machineDrafts.Count + metricDrafts.Count + statusDrafts.Count,
        };
    }
}
