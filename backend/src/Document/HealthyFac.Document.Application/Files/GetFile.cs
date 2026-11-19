using FluentValidation;
using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files;

public record GetFileRequest : IRequest<FileGridItem>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The machine of a machine file, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class GetFileRequestValidator : AbstractValidator<GetFileRequest>
{
    public GetFileRequestValidator()
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
public sealed class GetFilePreHandlerCheck(IDocumentDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetFileRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetFileRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Reads one attachment as a grid row. The company request reads any attachment of the company, also one that a
/// machine owns, as the company update does. The machine request reads only an attachment of that machine. An
/// embedded image, a deleted file, and a file of another company are "not found".
/// </summary>
/// <param name="dbContext">Reads the file ref.</param>
public sealed class GetFileRequestHandler(IDocumentDbContext dbContext) : IRequestHandler<GetFileRequest, FileGridItem>
{
    private readonly IDocumentDbContext _dbContext = dbContext;

    public async Task<FileGridItem> HandleAsync(
        GetFileRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var file = await _dbContext.FileRefs
                .AsNoTracking()
                .Where(candidate => candidate.Id == request.Id
                    && candidate.CompanyId == request.CompanyId
                    && candidate.Purpose == FilePurpose.Attachment
                    && (request.MachineId == null || candidate.MachineId == request.MachineId))
                .Select(FileGridItem.Projection)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(FileErrors.FileNotFound);

        return file;
    }
}
