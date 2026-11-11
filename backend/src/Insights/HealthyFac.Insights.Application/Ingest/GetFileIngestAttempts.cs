using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Ingest;

public record GetFileIngestAttemptsRequest : IRequest<FileIngestAttemptsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid FileId { get; init; }
}

public record FileIngestAttemptsResponse
{
    public required IReadOnlyList<IngestAttemptResponse> Items { get; init; }
}

public class GetFileIngestAttemptsRequestValidator : AbstractValidator<GetFileIngestAttemptsRequest>
{
    public GetFileIngestAttemptsRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(IngestErrors.CompanyIdRequired);
        RuleFor(x => x.FileId).NotEmpty().WithMessage(IngestErrors.FileIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetFileIngestAttemptsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetFileIngestAttemptsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetFileIngestAttemptsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the attempts of a file of the company, newest first, up to <see cref="IngestRules.MaxListedAttempts"/>. A
/// file of another company has no attempts of the company, so the answer is an empty list.
/// </summary>
/// <param name="dbContext">Reads the attempts.</param>
public sealed class GetFileIngestAttemptsRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<GetFileIngestAttemptsRequest, FileIngestAttemptsResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<FileIngestAttemptsResponse> HandleAsync(
        GetFileIngestAttemptsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var items = await _dbContext.IngestAttempts
            .Where(attempt => attempt.FileId == request.FileId && attempt.CompanyId == request.CompanyId)
            .OrderByDescending(attempt => attempt.QueuedAt)
            .ThenByDescending(attempt => attempt.Id)
            .Take(IngestRules.MaxListedAttempts)
            .Select(IngestAttemptResponse.Projection)
            .ToListAsync(cancellationToken);

        return new FileIngestAttemptsResponse { Items = items };
    }
}
