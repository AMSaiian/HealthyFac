using FluentValidation;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Ingest;

public record GetIngestFileLinkRequest : IRequest<IngestFileLinkResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid FileId { get; init; }

    /// <summary>
    /// Who reads the link. The entry point of the call sets it, never the caller input.
    /// </summary>
    public required FileLinkAudience Audience { get; init; }
}

/// <summary>
/// The file data and one read link of the file, for the ingest worker.
/// </summary>
public record IngestFileLinkResponse
{
    public required IngestFileResponse File { get; init; }

    public required string Url { get; init; }

    public required DateTime ExpiresAt { get; init; }
}

public class GetIngestFileLinkRequestValidator : AbstractValidator<GetIngestFileLinkRequest>
{
    public GetIngestFileLinkRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);
        RuleFor(x => x.FileId).NotEmpty().WithMessage(FileErrors.FileIdRequired);
        RuleFor(x => x.Audience).IsInEnum().WithMessage(FileErrors.LinkAudienceInvalid);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetIngestFileLinkPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetIngestFileLinkRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetIngestFileLinkRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the data of one attachment of the company and signs one read link for it, with the issuer and the lifetime
/// of <c>POST /v1/files/links</c>. The audience picks the host. A file of another company, a deleted file, an unknown
/// file, and an embedded image are "not found".
/// </summary>
/// <param name="dbContext">Reads the file ref.</param>
/// <param name="linkIssuer">Signs the link.</param>
public sealed class GetIngestFileLinkRequestHandler(IDocumentDbContext dbContext, IFileLinkIssuer linkIssuer)
    : IRequestHandler<GetIngestFileLinkRequest, IngestFileLinkResponse>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IFileLinkIssuer _linkIssuer = linkIssuer;

    public async Task<IngestFileLinkResponse> HandleAsync(
        GetIngestFileLinkRequest request,
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
        var link = _linkIssuer.Issue(file, request.Audience);

        return new IngestFileLinkResponse
        {
            File = IngestFileResponse.From(file),
            Url = link.Url,
            ExpiresAt = link.ExpiresAt,
        };
    }
}
