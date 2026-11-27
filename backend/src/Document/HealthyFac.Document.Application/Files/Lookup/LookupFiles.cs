using System.Security.Cryptography;
using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Lookup;

public record LookupFilesRequest : IRequest<LookupFilesResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>
    /// The encrypted file IDs. The handler decrypts them, so a bad value is left out and never fails. A request holds
    /// the IDs or <see cref="Search"/>, not both.
    /// </summary>
    [IgnoreIdEncryption]
    public IReadOnlyList<string>? Ids { get; init; }

    /// <summary>
    /// The start of the file name. With it, the lookup answers the first files by name, at most
    /// <see cref="FileValidationConstants.LookupSearchMaxItems"/>.
    /// </summary>
    public string? Search { get; init; }
}

public record LookupFilesResponse
{
    public required IReadOnlyList<LookedUpFile> Files { get; init; }
}

/// <summary>
/// The data of one attachment that another service shows: the name, the type, the size, and the machine.
/// </summary>
public record LookedUpFile
{
    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>The machine of a machine file, or <see langword="null"/> for a company file.</summary>
    public Guid? MachineId { get; init; }
}

public class LookupFilesRequestValidator : AbstractValidator<LookupFilesRequest>
{
    public LookupFilesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(request => request)
            .Must(request => request.Ids is null != request.Search is null)
            .WithMessage(FileErrors.LookupIdsOrSearchRequired);

        RuleFor(request => request.Ids!.Count)
            .LessThanOrEqualTo(FileValidationConstants.LookupIdsMaxCount)
            .WithMessage(FileErrors.LookupIdsTooMany)
            .When(request => request.Ids is not null);

        RuleForEach(request => request.Ids).SingleLineText();

        RuleFor(request => request.Search).SingleLineText();
    }
}

public sealed class LookupFilesPreHandlerCheck : PassThroughPreHandlerCheck<LookupFilesRequest>;

/// <summary>
/// Reads the data of the attachments for the given IDs, or the first attachments whose name starts with the search
/// text, sorted by name. The query holds the scope: a file of another company, a
/// deleted file, an embedded image, a machine file whose machine belongs to another company, and a value that is not an
/// encrypted ID are left out. The answer never shows why an ID is missing.
/// </summary>
/// <param name="dbContext">Reads the file refs and the machine registrations.</param>
/// <param name="idEncryptor">Decrypts the IDs.</param>
public sealed class LookupFilesRequestHandler(IDocumentDbContext dbContext, IIdEncryptor idEncryptor)
    : IRequestHandler<LookupFilesRequest, LookupFilesResponse>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IIdEncryptor _idEncryptor = idEncryptor;

    public async Task<LookupFilesResponse> HandleAsync(
        LookupFilesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var fileIds = (request.Ids ?? []).Select(TryDecrypt).OfType<Guid>().Distinct().ToList();
        var search = request.Search;

        var query =
            from file in _dbContext.FileRefs.AsNoTracking()
            where file.CompanyId == request.CompanyId
                && file.Purpose == FilePurpose.Attachment
                && (search != null ? file.FileName.StartsWith(search) : fileIds.Contains(file.Id))
            join registration in _dbContext.MachineRegistrations on file.MachineId equals registration.Id
                into registrations
            from registration in registrations.DefaultIfEmpty()
            where file.MachineId == null || (registration != null && registration.CompanyId == request.CompanyId)
            select new LookedUpFile
            {
                Id = file.Id,
                FileName = file.FileName,
                Extension = file.Extension,
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                MachineId = file.MachineId,
            };

        var files = search is null
            ? await query.ToListAsync(cancellationToken)
            : await query
                .OrderBy(file => file.FileName)
                .ThenBy(file => file.Id)
                .Take(FileValidationConstants.LookupSearchMaxItems)
                .ToListAsync(cancellationToken);

        return new LookupFilesResponse { Files = files };
    }

    private Guid? TryDecrypt(string encryptedId)
    {
        try
        {
            return _idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or CryptographicException)
        {
            return null;
        }
    }
}
