using FluentValidation;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Ingest;

public record CancelIngestAttemptRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public class CancelIngestAttemptRequestValidator : AbstractValidator<CancelIngestAttemptRequest>
{
    public CancelIngestAttemptRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(IngestErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(IngestErrors.AttemptIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class CancelIngestAttemptPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CancelIngestAttemptRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CancelIngestAttemptRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Stops a queued or running attempt of the company with the reason <c>cancelled</c> and queues the
/// <see cref="HealthyFac.Insights.Messaging.Ingest.IngestStateChanged"/> message in the same save. An attempt of
/// another company is "not found", and a final attempt is a conflict. The row version of the attempt, which the load
/// reads, makes a parallel change of the worker end in a conflict (409) and not in a lost update.
/// </summary>
/// <param name="dbContext">Loads the attempt and saves the change.</param>
/// <param name="outbox">Stores the message with the change.</param>
/// <param name="timeProvider">Provides the time of the change.</param>
public sealed class CancelIngestAttemptRequestHandler(
    IInsightsDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<CancelIngestAttemptRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        CancelIngestAttemptRequest request,
        CancellationToken cancellationToken
    )
    {
        var attempt = await _dbContext.IngestAttempts.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(IngestErrors.AttemptNotFound);

        if (attempt.IsFinal)
        {
            throw new ConflictException(IngestErrors.IngestFinal);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        attempt.Interrupt(now, IngestInterruptReason.Cancelled);

        var failedCount = await _dbContext.IngestAttempts.CountAsync(
            candidate => candidate.FileId == attempt.FileId
                && candidate.CompanyId == attempt.CompanyId
                && candidate.State == IngestState.Failed,
            cancellationToken);
        _outbox.Add(IngestStateMessages.Create(attempt, failedCount, now));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(IngestErrors.IngestChanged, exception);
        }
    }
}
