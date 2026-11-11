using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Machines;

public record UntieFileFromMachineRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The row version that the client read. The save fails when the file changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class UntieFileFromMachineRequestValidator : AbstractValidator<UntieFileFromMachineRequest>
{
    public UntieFileFromMachineRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(FileErrors.FileIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class UntieFileFromMachinePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UntieFileFromMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(UntieFileFromMachineRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Removes the tie of an attachment of the company to its machine. The handler sets the original row version from
/// the request, so a parallel change ends in the conflict (409). The same save stores a
/// <see cref="FileMachineChanged"/> message without a machine. An embedded image is "not found".
/// </summary>
/// <param name="dbContext">Loads the file ref and saves the change.</param>
/// <param name="outbox">Stores the message with the change.</param>
/// <param name="timeProvider">Provides the time of the change.</param>
public sealed class UntieFileFromMachineRequestHandler(
    IDocumentDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<UntieFileFromMachineRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        UntieFileFromMachineRequest request,
        CancellationToken cancellationToken
    )
    {
        var file = await _dbContext.FileRefs
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id
                        && candidate.CompanyId == request.CompanyId
                        && candidate.Purpose == FilePurpose.Attachment,
                    cancellationToken)
            ?? throw new NotFoundException(FileErrors.FileNotFound);

        var changedAt = _timeProvider.GetUtcNow().UtcDateTime;
        _dbContext.Entry(file).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        file.Untie(changedAt);
        _outbox.Add(
            new FileMachineChanged
            {
                FileId = file.Id,
                CompanyId = file.CompanyId,
                MachineId = null,
                ChangedAt = changedAt,
            });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(FileErrors.FileChanged, exception);
        }
    }
}
