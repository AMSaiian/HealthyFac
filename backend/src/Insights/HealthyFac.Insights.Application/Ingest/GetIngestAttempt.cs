using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Ingest;

public record GetIngestAttemptRequest : IRequest<IngestAttemptResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public class GetIngestAttemptRequestValidator : AbstractValidator<GetIngestAttemptRequest>
{
    public GetIngestAttemptRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(IngestErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(IngestErrors.AttemptIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetIngestAttemptPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetIngestAttemptRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetIngestAttemptRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one attempt of the company. An attempt of another company is "not found".
/// </summary>
/// <param name="dbContext">Reads the attempt.</param>
public sealed class GetIngestAttemptRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<GetIngestAttemptRequest, IngestAttemptResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<IngestAttemptResponse> HandleAsync(
        GetIngestAttemptRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.IngestAttempts
            .Where(attempt => attempt.Id == request.Id && attempt.CompanyId == request.CompanyId)
            .Select(IngestAttemptResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException(IngestErrors.AttemptNotFound);
    }
}
