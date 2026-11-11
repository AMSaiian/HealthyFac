using System.Security.Cryptography;
using FluentValidation;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Document.Application.Files.Links;

public record GetFileLinksRequest : IRequest<GetFileLinksResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>
    /// The encrypted file IDs. The handler decrypts them, so a bad value is left out and never fails.
    /// </summary>
    [IgnoreIdEncryption]
    public required IReadOnlyList<string> Ids { get; init; }
}

public record GetFileLinksResponse
{
    public required IReadOnlyList<FileLink> Links { get; init; }
}

public class GetFileLinksRequestValidator : AbstractValidator<GetFileLinksRequest>
{
    public GetFileLinksRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(FileErrors.CompanyIdRequired);

        RuleFor(x => x.Ids)
            .NotNull()
            .WithMessage(FileErrors.LinkIdsRequired)
            .Must(ids => ids.Count <= FileValidationConstants.LinkIdsMaxCount)
            .WithMessage(FileErrors.LinkIdsTooMany);

        RuleForEach(x => x.Ids).SingleLineText();
    }
}

public sealed class GetFileLinksPreHandlerCheck : PassThroughPreHandlerCheck<GetFileLinksRequest>;

/// <summary>
/// Issues a read link for each file that the actor may read. The query holds the scope: a file of another company, a
/// deleted file, and a machine file of another company are left out, as are values that are not
/// encrypted IDs. The answer never shows why an ID is missing.
/// </summary>
/// <remarks>
/// A link stays valid until it expires, also after the file is deleted or the actor loses access. The links are short
/// lived, which limits that window.
/// </remarks>
/// <param name="dbContext">Reads the file refs and the machine registrations.</param>
/// <param name="idEncryptor">Decrypts the IDs.</param>
/// <param name="linkIssuer">Signs the links.</param>
public sealed class GetFileLinksRequestHandler(
    IDocumentDbContext dbContext,
    IIdEncryptor idEncryptor,
    IFileLinkIssuer linkIssuer
) : IRequestHandler<GetFileLinksRequest, GetFileLinksResponse>
{
    private readonly IDocumentDbContext _dbContext = dbContext;
    private readonly IIdEncryptor _idEncryptor = idEncryptor;
    private readonly IFileLinkIssuer _linkIssuer = linkIssuer;

    public async Task<GetFileLinksResponse> HandleAsync(
        GetFileLinksRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var fileIds = request.Ids.Select(TryDecrypt).OfType<Guid>().Distinct().ToList();

        var files = await (
            from file in _dbContext.FileRefs.AsNoTracking()
            where fileIds.Contains(file.Id) && file.CompanyId == request.CompanyId
            join registration in _dbContext.MachineRegistrations on file.MachineId equals registration.Id
                into registrations
            from registration in registrations.DefaultIfEmpty()
            where file.MachineId == null
                || (registration != null
                    && registration.CompanyId == request.CompanyId)
            select file).ToListAsync(cancellationToken);

        return new GetFileLinksResponse
        {
            Links = [.. files.Select(file => _linkIssuer.Issue(file, FileLinkAudience.Public))],
        };
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
