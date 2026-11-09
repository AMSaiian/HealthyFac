using FluentValidation;
using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Document.Application.Files.Uploading;

public record UploadFileRequest : IRequest<UploadFileResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The machine of a machine file, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }

    public required FileUpload File { get; init; }

    public string? Description { get; init; }
}

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(FileErrors.MachineIdRequired)
            .When(x => x.MachineId.HasValue);

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage(FileErrors.FileRequired)
            .SetValidator(new FileUploadValidator());

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(FileValidationConstants.Size.AttachmentMaxBytes)
            .WithMessage(FileErrors.AttachmentTooLarge)
            .When(x => x.File is not null)
            .WithName(nameof(UploadFileRequest.File));

        RuleFor(x => x.Description).IsFileDescription();
    }
}

/// <summary>
/// Checks the company, then the machine scope when the request names a machine: a machine outside the company,
/// and a deleted machine, are "not found" (404). A company file has no further scope.
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class UploadFilePreHandlerCheck(IDocumentDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UploadFileRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(UploadFileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Stores a company file (no machine) or a file of a machine.
/// </summary>
/// <param name="uploader">Stores the file.</param>
public class UploadFileRequestHandler(IFileUploader uploader)
    : IRequestHandler<UploadFileRequest, UploadFileResponse>
{
    private readonly IFileUploader _uploader = uploader;

    public async Task<UploadFileResponse> HandleAsync(
        UploadFileRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var id = await _uploader.UploadAsync(
            request.CompanyId,
            request.MachineId,
            FilePurpose.Attachment,
            request.File,
            request.Description,
            cancellationToken);

        return new UploadFileResponse { Id = id };
    }
}
