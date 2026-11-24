using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public record DismissDefinitionDraftsRequest : IRequest<DismissDefinitionDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public Guid? MachineId { get; init; }

    public Guid? AttemptId { get; init; }
}

public record DismissDefinitionDraftsResponse
{
    public required int DismissedCount { get; init; }
}

public sealed class DismissDefinitionDraftsRequestValidator : AbstractValidator<DismissDefinitionDraftsRequest>
{
    public DismissDefinitionDraftsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);

        RuleFor(request => request.MachineId)
            .NotEqual(Guid.Empty)
            .WithMessage(DefinitionDraftErrors.MachineIdRequired)
            .When(request => request.MachineId is not null);

        RuleFor(request => request.AttemptId)
            .NotEqual(Guid.Empty)
            .WithMessage(DefinitionDraftErrors.AttemptIdRequired)
            .When(request => request.AttemptId is not null);

        RuleFor(request => request)
            .Must(request => request.MachineId is null != request.AttemptId is null)
            .WithMessage(DefinitionDraftErrors.SearchScopeRequired);
    }
}

/// <summary>
/// Checks the company of the actor and, for a machine scope, that the machine is an active registration of the
/// company (else "not found").
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DismissDefinitionDraftsPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<DismissDefinitionDraftsRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DismissDefinitionDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Dismisses every <c>pending</c> and <c>awaitingTarget</c> metric, status, and phase draft in the scope. The machine
/// scope
/// holds the drafts that target the machine. The attempt scope holds every draft of the attempt. One save stores all
/// changes, so a draft that changed in parallel is a conflict and nothing is dismissed.
/// </summary>
/// <param name="dbContext">Loads the drafts and saves the change.</param>
public sealed class DismissDefinitionDraftsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<DismissDefinitionDraftsRequest, DismissDefinitionDraftsResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    /// <summary>
    /// Loads the open drafts of the scope, dismisses them, and saves once.
    /// </summary>
    /// <param name="request">The company and the machine or the attempt.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The number of dismissed drafts.</returns>
    /// <exception cref="ConflictException">Thrown when a draft changed while the use case ran.</exception>
    public async Task<DismissDefinitionDraftsResponse> HandleAsync(
        DismissDefinitionDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var metricDrafts = await _dbContext.MetricDefinitionDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.MachineId == null || draft.TargetMachineId == request.MachineId)
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId))
            .Where(OpenDefinitionDrafts.Predicate<MetricDefinitionDraft>())
            .ToListAsync(cancellationToken);

        var statusDrafts = await _dbContext.StatusDefinitionDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.MachineId == null || draft.TargetMachineId == request.MachineId)
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId))
            .Where(OpenDefinitionDrafts.Predicate<StatusDefinitionDraft>())
            .ToListAsync(cancellationToken);

        var phaseDrafts = await _dbContext.OperatingPhaseDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.MachineId == null || draft.TargetMachineId == request.MachineId)
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId))
            .Where(OpenDefinitionDrafts.Predicate<OperatingPhaseDraft>())
            .ToListAsync(cancellationToken);

        metricDrafts.ForEach(draft => draft.Dismiss());
        statusDrafts.ForEach(draft => draft.Dismiss());
        phaseDrafts.ForEach(draft => draft.Dismiss());

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DefinitionDraftErrors.DraftChanged, exception);
        }

        return new DismissDefinitionDraftsResponse
            { DismissedCount = metricDrafts.Count + statusDrafts.Count + phaseDrafts.Count };
    }
}
