using FluentValidation;
using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Messaging.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Machines;

public record TieFileToMachineRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required Guid MachineId { get; init; }

    /// <summary>The row version that the client read. The save fails when the file changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class TieFileToMachineRequestValidator : AbstractValidator<TieFileToMachineRequest>
{
    public TieFileToMachineRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(FileErrors.FileIdRequired);
        RuleFor(x => x.MachineId).NotEmpty().WithMessage(FileErrors.MachineIdRequired);
    }
}

/// <summary>
/// Checks the company, then the registration of the machine: a machine outside the company, and a deleted machine, are
/// "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class TieFileToMachinePreHandlerCheck(IDocumentDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<TieFileToMachineRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(TieFileToMachineRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Ties an attachment of the company to a machine, whether the file was tied before or not. The handler sets the
/// original row version from the request, so a parallel change ends in the conflict (409). The same save stores a
/// <see cref="FileMachineChanged"/> message for the services that hold data of the file. An embedded image is "not
/// found".
/// </summary>
/// <param name="dbContext">Loads the file ref and saves the change.</param>
/// <param name="outbox">Stores the message with the change.</param>
/// <param name="timeProvider">Provides the time of the change.</param>
public sealed class TieFileToMachineRequestHandler(
    IDocumentDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<TieFileToMachineRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        TieFileToMachineRequest request,
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
        file.TieTo(request.MachineId, changedAt);
        _outbox.Add(
            new FileMachineChanged
            {
                FileId = file.Id,
                CompanyId = file.CompanyId,
                MachineId = request.MachineId,
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
