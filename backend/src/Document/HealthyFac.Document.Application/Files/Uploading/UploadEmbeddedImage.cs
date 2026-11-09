using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Document.Application.Files.Uploading;

public record UploadEmbeddedImageRequest : IRequest<UploadFileResponse>
{
    public required Guid CompanyId { get; init; }

    public required FileUpload File { get; init; }
}

public class UploadEmbeddedImageRequestValidator : AbstractValidator<UploadEmbeddedImageRequest>
{
    public UploadEmbeddedImageRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage(FileErrors.FileRequired)
            .SetValidator(new FileUploadValidator());

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(FileValidationConstants.Size.EmbeddedMaxBytes)
            .WithMessage(FileErrors.EmbeddedTooLarge)
            .When(x => x.File is not null)
            .WithName(nameof(UploadEmbeddedImageRequest.File));
    }
}

/// <summary>
/// Checks that the request names the company of the actor. An embedded image has no further scope.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class UploadEmbeddedImagePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UploadEmbeddedImageRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(UploadEmbeddedImageRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Stores an image of a rich text description. The image belongs to the company, has no machine, and shows in no grid.
/// </summary>
/// <param name="uploader">Stores the file.</param>
public class UploadEmbeddedImageRequestHandler(IFileUploader uploader)
    : IRequestHandler<UploadEmbeddedImageRequest, UploadFileResponse>
{
    private readonly IFileUploader _uploader = uploader;

    public async Task<UploadFileResponse> HandleAsync(
        UploadEmbeddedImageRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var id = await _uploader.UploadAsync(
            request.CompanyId,
            machineId: null,
            FilePurpose.Embedded,
            request.File,
            description: null,
            cancellationToken);

        return new UploadFileResponse { Id = id };
    }
}
