using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public record GetAttemptDefinitionDraftsRequest : IRequest<AttemptDefinitionDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid AttemptId { get; init; }
}

public record AttemptDefinitionDraftsResponse
{
    public required IReadOnlyList<MetricDraftDetail> Metrics { get; init; }

    public required IReadOnlyList<StatusDraftDetail> Statuses { get; init; }
}

public sealed class GetAttemptDefinitionDraftsRequestValidator : AbstractValidator<GetAttemptDefinitionDraftsRequest>
{
    public GetAttemptDefinitionDraftsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);
        RuleFor(request => request.AttemptId).NotEmpty().WithMessage(DefinitionDraftErrors.AttemptIdRequired);
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
/// Reads every metric and status draft of one ingest attempt (all states) with all their values. The Gateway groups
/// them by machine. An attempt that Telemetry does not know gives empty lists, because Telemetry does not own attempts.
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
            .Where(draft => draft.CompanyId == request.CompanyId && draft.AttemptId == request.AttemptId)
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.StatusDefinitionDrafts
            .AsNoTracking()
            .Where(draft => draft.CompanyId == request.CompanyId && draft.AttemptId == request.AttemptId)
            .ToListAsync(cancellationToken);

        return new AttemptDefinitionDraftsResponse
        {
            Metrics = [.. metrics.Select(MetricDraftDetail.From)],
            Statuses = [.. statuses.Select(StatusDraftDetail.From)],
        };
    }
}
