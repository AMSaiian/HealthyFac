using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Ingest;

public record GetIngestFileRequest : IRequest<IngestFileResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid FileId { get; init; }
}

/// <summary>
/// The file data that the ingest reads through the gRPC service.
/// </summary>
public record IngestFileResponse
{
    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    public Guid? MachineId { get; init; }

    /// <summary>
    /// The time of the last tie change (UTC), or the creation time of the file when it was never tied.
    /// </summary>
    public required DateTime MachineChangedAt { get; init; }

    public required string Purpose { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required long SizeBytes { get; init; }

    public static IngestFileResponse From(FileRef file) =>
        new()
        {
            FileId = file.Id,
            CompanyId = file.CompanyId,
            MachineId = file.MachineId,
            MachineChangedAt = file.MachineChangedAt ?? file.CreatedAt,
            Purpose = file.Purpose.Value,
            FileName = file.FileName,
            Extension = file.Extension,
            SizeBytes = file.SizeBytes,
        };
}

public class GetIngestFileRequestValidator : AbstractValidator<GetIngestFileRequest>
{
    public GetIngestFileRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);
        RuleFor(x => x.FileId).NotEmpty().WithMessage(FileErrors.FileIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetIngestFilePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetIngestFileRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetIngestFileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the data of one attachment of the company. A file of another company, a deleted file, an unknown file, and
/// an embedded image are "not found".
/// </summary>
/// <param name="dbContext">Reads the file ref.</param>
public sealed class GetIngestFileRequestHandler(IDocumentDbContext dbContext)
    : IRequestHandler<GetIngestFileRequest, IngestFileResponse>
{
    private readonly IDocumentDbContext _dbContext = dbContext;

    public async Task<IngestFileResponse> HandleAsync(
        GetIngestFileRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var file = await _dbContext.FileRefs
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.FileId
                        && candidate.CompanyId == request.CompanyId
                        && candidate.Purpose == FilePurpose.Attachment,
                    cancellationToken)
            ?? throw new NotFoundException(FileErrors.FileNotFound);

        return IngestFileResponse.From(file);
    }
}
