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

namespace HealthyFac.Document.Application.Files;

public record DeleteFileRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    /// <summary>The machine of a machine file, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class DeleteFileRequestValidator : AbstractValidator<DeleteFileRequest>
{
    public DeleteFileRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(FileErrors.MachineIdRequired)
            .When(x => x.MachineId.HasValue);

        RuleFor(x => x.Id).NotEmpty().WithMessage(FileErrors.FileIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope when the request names a machine: a machine outside the company,
/// and a deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DeleteFilePreHandlerCheck(IDocumentDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteFileRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DeleteFileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Removes a company file or a file of a machine. The save interceptor turns the removal into a soft delete, so the
/// row stays and the query filter hides it. The blob stays too: the service has no blob cleanup yet. A second delete
/// is "not found". The same save stores a <see cref="FileDeleted"/> message for the services that hold data of the
/// file.
/// </summary>
/// <param name="dbContext">Loads the file ref and saves the removal.</param>
/// <param name="outbox">Stores the message with the removal.</param>
public sealed class DeleteFileRequestHandler(IDocumentDbContext dbContext, IOutbox outbox)
    : RequestHandler<DeleteFileRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    protected override async Task HandleInternalAsync(
        DeleteFileRequest request,
        CancellationToken cancellationToken
    )
    {
        var file = await _dbContext.FileRefs
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id
                        && candidate.CompanyId == request.CompanyId
                        && candidate.Purpose == FilePurpose.Attachment
                        && (request.MachineId == null || candidate.MachineId == request.MachineId),
                    cancellationToken)
            ?? throw new NotFoundException(FileErrors.FileNotFound);

        _dbContext.FileRefs.Remove(file);
        _outbox.Add(new FileDeleted { FileId = file.Id, CompanyId = file.CompanyId });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
