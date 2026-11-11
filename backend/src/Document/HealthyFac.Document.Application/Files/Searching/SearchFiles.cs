using FluentValidation;
using HealthyFac.Document.Application.MachineRegistrations;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Searching;

public record SearchFilesRequest : GridRequest<FileGridFilter, FileSortColumn>,
    IRequest<GridResponse<FileGridItem>>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The machine of a machine file, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }
}

public sealed class SearchFilesRequestValidator
    : GridRequestValidator<SearchFilesRequest, FileGridFilter, FileSortColumn>
{
    public SearchFilesRequestValidator()
        : base(FileSortColumn.All)
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(FileErrors.MachineIdRequired)
            .When(request => request.MachineId.HasValue);

        RuleFor(request => request.Filter)
            .SetValidator(new FileGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

/// <summary>
/// Checks the company, then the machine scope when the request names a machine: a machine outside the company,
/// and a deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class SearchFilesPreHandlerCheck(IDocumentDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchFilesRequest>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(SearchFilesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is { } machineId)
        {
            await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, machineId, cancellationToken);
        }
    }
}

/// <summary>
/// Reads one page of the attachments of the company, tied or not (a request without a machine), or of one machine.
/// Embedded images and deleted files stay out. The pre-handler check has already checked the machine scope, so a
/// machine outside the scope is "not found" and never an empty page. The newest upload comes first unless the
/// request names a sort.
/// </summary>
/// <param name="dbContext">Reads the file refs.</param>
public sealed class SearchFilesRequestHandler(IDocumentDbContext dbContext)
    : IRequestHandler<SearchFilesRequest, GridResponse<FileGridItem>>
{
    private readonly IDocumentDbContext _dbContext = dbContext;

    public async Task<GridResponse<FileGridItem>> HandleAsync(
        SearchFilesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.FileRefs
            .AsNoTracking()
            .Where(file => file.CompanyId == request.CompanyId
                && (request.MachineId == null || file.MachineId == request.MachineId)
                && file.Purpose == FilePurpose.Attachment)
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort ?? FileSearchDefaults.Sort, FileSortMap.Instance)
            .ToGridResponseAsync(request.Offset, request.Limit, FileGridItem.Projection, cancellationToken);

        return response;
    }
}
