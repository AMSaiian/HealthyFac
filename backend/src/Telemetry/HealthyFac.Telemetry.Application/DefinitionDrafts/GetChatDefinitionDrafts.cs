using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public record GetChatDefinitionDraftsRequest : IRequest<ChatDefinitionDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid ChatId { get; init; }
}

public record ChatDefinitionDraftsResponse
{
    public required IReadOnlyList<MetricDraftDetail> Metrics { get; init; }

    public required IReadOnlyList<StatusDraftDetail> Statuses { get; init; }

    public IReadOnlyList<PhaseDraftDetail> Phases { get; init; } = [];
}

public sealed class GetChatDefinitionDraftsRequestValidator : AbstractValidator<GetChatDefinitionDraftsRequest>
{
    public GetChatDefinitionDraftsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);
        RuleFor(request => request.ChatId).NotEmpty().WithMessage(DefinitionDraftErrors.ChatIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetChatDefinitionDraftsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetChatDefinitionDraftsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetChatDefinitionDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads every metric, status, and phase draft of one chat (all states) with all their values. The
/// Gateway groups
/// them by machine. A chat that Telemetry does not know gives empty lists, because Telemetry does not own chats.
/// </summary>
/// <param name="dbContext">Reads the drafts.</param>
public sealed class GetChatDefinitionDraftsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetChatDefinitionDraftsRequest, ChatDefinitionDraftsResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<ChatDefinitionDraftsResponse> HandleAsync(
        GetChatDefinitionDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var metrics = await _dbContext.MetricDefinitionDrafts
            .AsNoTracking()
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId && draft.Proposal.ChatId == request.ChatId)
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.StatusDefinitionDrafts
            .AsNoTracking()
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId && draft.Proposal.ChatId == request.ChatId)
            .ToListAsync(cancellationToken);

        var phases = await _dbContext.OperatingPhaseDrafts
            .AsNoTracking()
            .Include(draft => draft.Conditions)
            .Include(draft => draft.Limits)
            .Include(draft => draft.Proposal)
            .Where(draft => draft.CompanyId == request.CompanyId && draft.Proposal.ChatId == request.ChatId)
            .ToListAsync(cancellationToken);

        return new ChatDefinitionDraftsResponse
        {
            Phases = [.. phases.Select(PhaseDraftDetail.From)],
            Metrics = [.. metrics.Select(MetricDraftDetail.From)],
            Statuses = [.. statuses.Select(StatusDraftDetail.From)],
        };
    }
}
