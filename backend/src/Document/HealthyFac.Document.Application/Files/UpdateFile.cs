using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files;

public record UpdateFileRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    /// <summary>The machine of a machine file, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }

    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public string? Description { get; init; }

    /// <summary>The row version that the client read. The save fails when the file changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class UpdateFileRequestValidator : AbstractValidator<UpdateFileRequest>
{
    public UpdateFileRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(FileErrors.MachineIdRequired)
            .When(x => x.MachineId.HasValue);

        RuleFor(x => x.Id).NotEmpty().WithMessage(FileErrors.FileIdRequired);

        RuleFor(x => x.FileName).IsFileNameInput();

        RuleFor(x => x.Description).IsFileDescription(richTextPolicy);
    }
}

/// <summary>
/// Checks the company, then the machine scope when the request names a machine: a machine outside the company,
/// and a deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class UpdateFilePreHandlerCheck(IDocumentDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UpdateFileRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(UpdateFileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Renames a company file or a file of a machine and changes its description. The handler sets the original row
/// version from the request, so a parallel change ends in the conflict (409). The stored extension never changes: a
/// trailing <c>.&lt;extension&gt;</c> in the typed name is removed first.
/// </summary>
/// <param name="dbContext">Loads the file ref and saves the change.</param>
public sealed class UpdateFileRequestHandler(IDocumentDbContext dbContext)
    : RequestHandler<UpdateFileRequest>
{
    private const string FileNameKey = "fileName";

    private readonly IDocumentDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        UpdateFileRequest request,
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

        var fileName = FileNames.StripStoredExtension(request.FileName, file.Extension);
        if (fileName.Length == 0)
        {
            throw new AppValidationException([new ValidationFailure(FileNameKey, FileErrors.NameRequired)]);
        }

        _dbContext.Entry(file).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        file.Rename(fileName, request.Description);

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
