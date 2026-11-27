using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public record GetAttemptDefinitionDraftsRequest : IRequest<AttemptDefinitionDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? ChatId { get; init; }
}

public record AttemptDefinitionDraftsResponse
{
    public required IReadOnlyList<MetricDraftDetail> Metrics { get; init; }

    public required IReadOnlyList<StatusDraftDetail> Statuses { get; init; }

    public IReadOnlyList<PhaseDraftDetail> Phases { get; init; } = [];
}

public sealed class GetAttemptDefinitionDraftsRequestValidator : AbstractValidator<GetAttemptDefinitionDraftsRequest>
{
    public GetAttemptDefinitionDraftsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);

        RuleFor(request => request.AttemptId)
            .NotEqual(Guid.Empty)
            .WithMessage(DefinitionDraftErrors.AttemptIdRequired)
            .When(request => request.AttemptId is not null);

        RuleFor(request => request.ChatId)
            .NotEqual(Guid.Empty)
            .WithMessage(DefinitionDraftErrors.ChatIdRequired)
            .When(request => request.ChatId is not null);

        RuleFor(request => request)
            .Must(request => request.AttemptId is null != request.ChatId is null)
            .WithMessage(DefinitionDraftErrors.GetScopeRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetAttemptDefinitionDraftsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetAttemptDefinitionDraftsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetAttemptDefinitionDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads every metric, status, and phase draft of one ingest attempt or of one chat (all states) with all their
/// values. The Gateway groups them by machine. An attempt or a chat that Telemetry does not know gives empty lists,
/// because Telemetry does not own attempts or chats.
/// </summary>
/// <param name="dbContext">Reads the drafts.</param>
public sealed class GetAttemptDefinitionDraftsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetAttemptDefinitionDraftsRequest, AttemptDefinitionDraftsResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<AttemptDefinitionDraftsResponse> HandleAsync(
        GetAttemptDefinitionDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var metrics = await _dbContext.MetricDefinitionDrafts
            .AsNoTracking()
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId)
                && (request.ChatId == null || draft.Proposal.ChatId == request.ChatId))
            .ToListAsync(cancellationToken);

        var statuses = await _dbContext.StatusDefinitionDrafts
            .AsNoTracking()
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId)
                && (request.ChatId == null || draft.Proposal.ChatId == request.ChatId))
            .ToListAsync(cancellationToken);

        var phases = await _dbContext.OperatingPhaseDrafts
            .AsNoTracking()
            .Include(draft => draft.Conditions)
            .Include(draft => draft.Limits)
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId)
                && (request.ChatId == null || draft.Proposal.ChatId == request.ChatId))
            .ToListAsync(cancellationToken);

        return new AttemptDefinitionDraftsResponse
        {
            Phases = [.. phases.Select(PhaseDraftDetail.From)],
            Metrics = [.. metrics.Select(MetricDraftDetail.From)],
            Statuses = [.. statuses.Select(StatusDraftDetail.From)],
        };
    }
}
