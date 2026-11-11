using System.Diagnostics;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Ingest;

public record StartIngestAttemptRequest : IRequest<StartIngestAttemptResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid FileId { get; init; }
}

public record StartIngestAttemptResponse
{
    public required Guid Id { get; init; }
}

public class StartIngestAttemptRequestValidator : AbstractValidator<StartIngestAttemptRequest>
{
    public StartIngestAttemptRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(IngestErrors.CompanyIdRequired);
        RuleFor(x => x.FileId).NotEmpty().WithMessage(IngestErrors.FileIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class StartIngestAttemptPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<StartIngestAttemptRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(StartIngestAttemptRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Queues an ingest attempt for an attachment of the company. The handler reads the file from Document on behalf of
/// the user, so a file of another company, a deleted file, and an embedded image are "not found". A file with a queued
/// or running attempt, a completed attempt, or the limit of failed attempts is a conflict. The attempt keeps a
/// snapshot of the file and the trace context of the request. One save stores the attempt and the
/// <see cref="HealthyFac.Insights.Messaging.Ingest.IngestStateChanged"/> message.
/// </summary>
/// <remarks>
/// The unique index of the active attempt of a file settles a race of two starts. The row inserted here is queued, so
/// that index is the only unique constraint that the save can break.
/// </remarks>
/// <param name="dbContext">Loads the earlier attempts and saves the new one.</param>
/// <param name="fileCatalog">Reads the file from Document.</param>
/// <param name="outbox">Stores the message with the change.</param>
/// <param name="timeProvider">Provides the time when the user queued the attempt.</param>
public sealed class StartIngestAttemptRequestHandler(
    IInsightsDbContext dbContext,
    IFileCatalog fileCatalog,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<StartIngestAttemptRequest, StartIngestAttemptResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IFileCatalog _fileCatalog = fileCatalog;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<StartIngestAttemptResponse> HandleAsync(
        StartIngestAttemptRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var file = await _fileCatalog.GetFileForUserAsync(request.FileId, cancellationToken);
        if (file is null || file.CompanyId != request.CompanyId || file.Purpose != IngestRules.AttachmentPurpose)
        {
            throw new NotFoundException(IngestErrors.FileNotFound);
        }

        if (!IngestRules.IngestableExtensions.Contains(file.Extension))
        {
            throw new AppValidationException(
                [new ValidationFailure(IngestErrors.FileTypeField, IngestErrors.FileTypeNotIngestable)]);
        }

        var states = await _dbContext.IngestAttempts
            .AsNoTracking()
            .Where(attempt => attempt.FileId == file.FileId && attempt.CompanyId == request.CompanyId)
            .Select(attempt => attempt.State)
            .ToListAsync(cancellationToken);
        var failedCount = states.Count(state => state == IngestState.Failed);
        EnsureCanStart(states, failedCount);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var attempt = new IngestAttempt(
            request.CompanyId,
            file.FileId,
            file.MachineId,
            file.MachineChangedAt,
            file.FileName,
            file.Extension,
            file.SizeBytes,
            now,
            Activity.Current?.Id);
        _dbContext.IngestAttempts.Add(attempt);
        _outbox.Add(IngestStateMessages.Create(attempt, failedCount, now));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(IngestErrors.IngestActive, exception);
        }

        return new StartIngestAttemptResponse { Id = attempt.Id };
    }

    private static void EnsureCanStart(List<IngestState> states, int failedCount)
    {
        if (states.Any(state => state == IngestState.Queued || state == IngestState.InProcess))
        {
            throw new ConflictException(IngestErrors.IngestActive);
        }

        if (states.Contains(IngestState.Completed))
        {
            throw new ConflictException(IngestErrors.IngestCompleted);
        }

        if (failedCount >= IngestRules.MaxFailedAttempts)
        {
            throw new ConflictException(IngestErrors.IngestFailedLimit);
        }
    }
}
